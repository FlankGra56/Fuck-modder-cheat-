using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using AntiCheatService.Core.Models;
using AntiCheatService.Infrastructure.Database;

namespace AntiCheatService.Core.Detection;

public class TelemetryAnalyzer : ITelemetryAnalyzer
{
    private readonly AntiCheatDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<TelemetryAnalyzer> _logger;

    private static readonly Dictionary<string, int> ExpectedTelemetryFrequencies = new()
    {
        { "SendTssSdkAntiDataToLobby", 5 },
        { "SendDSHawkEyePatrolLogToLobby", 10 },
        { "SendSecTLog", 15 },
    };

    public TelemetryAnalyzer(AntiCheatDbContext db, IConnectionMultiplexer redis, ILogger<TelemetryAnalyzer> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    public async Task<TelemetryAnalysisResult> AnalyzeTelemetryGapAsync(string playerId, int timeWindowSeconds = 60)
    {
        var result = new TelemetryAnalysisResult();
        var cutoffTime = DateTime.UtcNow.AddSeconds(-timeWindowSeconds);

        try
        {
            var events = await _db.TelemetryEvents
                .Where(e => e.Player!.PlayerUID == playerId && e.Timestamp >= cutoffTime)
                .GroupBy(e => e.EventName)
                .Select(g => new { EventName = g.Key, Count = g.Count() })
                .ToListAsync();

            var missingCount = 0;

            foreach (var expectedEvent in ExpectedTelemetryFrequencies)
            {
                var receivedEvent = events.FirstOrDefault(e => e.EventName == expectedEvent.Key);
                var receivedCount = receivedEvent?.Count ?? 0;
                var expectedCount = (int)Math.Ceiling((double)timeWindowSeconds / expectedEvent.Value);

                if (receivedCount == 0)
                {
                    missingCount++;
                    result.MissingEvents.Add(new MissingEvent
                    {
                        EventName = expectedEvent.Key,
                        ExpectedCount = expectedCount,
                        ReceivedCount = 0,
                        ConfidencePercentage = 99.0
                    });
                }
            }

            if (missingCount >= 3)
            {
                result.IsCheatDetected = true;
                result.ConfidenceScore = 99.8;
                result.Evidence = $"Detected {missingCount} missing telemetry events";
                _logger.LogWarning($"🚨 CHEAT DETECTED: {playerId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in telemetry analysis");
        }

        return result;
    }

    public async Task RecordTelemetryEventAsync(string playerId, string eventName, string? eventData = null)
    {
        try
        {
            var player = await _db.Players.FirstOrDefaultAsync(p => p.PlayerUID == playerId);
            if (player == null)
            {
                player = new Player { PlayerUID = playerId, PlayerName = playerId };
                _db.Players.Add(player);
                await _db.SaveChangesAsync();
            }

            var telemetryEvent = new TelemetryEvent
            {
                PlayerId = player.Id,
                EventName = eventName,
                Timestamp = DateTime.UtcNow
            };

            _db.TelemetryEvents.Add(telemetryEvent);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording telemetry");
        }
    }
}
