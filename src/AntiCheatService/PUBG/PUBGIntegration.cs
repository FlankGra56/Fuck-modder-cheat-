using AntiCheatService.Core.Models;
using AntiCheatService.Infrastructure.Database;

namespace AntiCheatService.PUBG;

/// <summary>
/// PUBG Mobile Global 64-bit Specific Integration
/// Handles PUBG-specific cheat detection and ban management
/// </summary>
public interface IPUBGIntegration
{
    Task<bool> ValidatePUBGPlayerAsync(string pubgUID);
    Task<PUBGPlayerProfile> GetPlayerProfileAsync(string pubgUID);
    Task<bool> ReportCheatDetectionAsync(string pubgUID, PUBGCheatReport report);
    Task<bool> BanPlayerImmediateAsync(string pubgUID, PUBGBanReason reason);
}

public class PUBGIntegration : IPUBGIntegration
{
    private readonly AntiCheatDbContext _db;
    private readonly ILogger<PUBGIntegration> _logger;

    // PUBG-specific telemetry events
    private static readonly Dictionary<string, PUBGTelemetryRequirement> PUBGRequiredTelemetry = new()
    {
        { "SendTssSdkAntiDataToLobby", new() { 
            RequiredFrequency = 5, 
            Severity = "CRITICAL", 
            Description = "Anti-cheat telemetry to server" 
        }},
        { "SendDSHawkEyePatrolLogToLobby", new() { 
            RequiredFrequency = 10, 
            Severity = "CRITICAL", 
            Description = "Hawk-eye monitoring system" 
        }},
        { "SendSecTLog", new() { 
            RequiredFrequency = 15, 
            Severity = "HIGH", 
            Description = "Security telemetry log" 
        }},
        { "SendActivityTLog", new() { 
            RequiredFrequency = 20, 
            Severity = "MEDIUM", 
            Description = "Activity tracking log" 
        }},
        { "OnPlayerRPCValidateFailed", new() { 
            RequiredFrequency = 0, 
            Severity = "HIGH", 
            Description = "RPC validation failure (should not occur)" 
        }},
        { "OnPlayerActorChannelError", new() { 
            RequiredFrequency = 0, 
            Severity = "HIGH", 
            Description = "Channel error (should not occur)" 
        }},
    };

    // Known cheat signatures for PUBG Mobile
    private static readonly List<string> KnownCheatSignatures = new()
    {
        "BRPlayerCharacterBase.lua",
        "AKModBypassInitialized",
        "require_override",
        "GC.collect_override",
        "SharedVisualAssistanceOwner",
        "GameplayCallbacks.hijack",
    };

    public PUBGIntegration(AntiCheatDbContext db, ILogger<PUBGIntegration> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> ValidatePUBGPlayerAsync(string pubgUID)
    {
        try
        {
            // Validate PUBG UID format
            if (string.IsNullOrEmpty(pubgUID) || pubgUID.Length < 10)
            {
                _logger.LogWarning($"Invalid PUBG UID format: {pubgUID}");
                return false;
            }

            var player = await _db.Players.FirstOrDefaultAsync(p => p.PlayerUID == pubgUID);
            return player != null && player.Status == PlayerStatus.Active;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PUBG player");
            return false;
        }
    }

    public async Task<PUBGPlayerProfile> GetPlayerProfileAsync(string pubgUID)
    {
        try
        {
            var player = await _db.Players
                .Include(p => p.Bans)
                .Include(p => p.CheatDetections)
                .FirstOrDefaultAsync(p => p.PlayerUID == pubgUID);

            if (player == null)
                return new PUBGPlayerProfile { IsValid = false };

            var activeBan = player.Bans.FirstOrDefault(b => b.UnbannedAt == null);
            var detections = player.CheatDetections.OrderByDescending(d => d.DetectedAt).Take(10).ToList();

            return new PUBGPlayerProfile
            {
                IsValid = true,
                PUBGUID = pubgUID,
                PlayerName = player.PlayerName,
                Status = player.Status.ToString(),
                CreatedAt = player.CreatedAt,
                LastSeen = player.LastSeen,
                IsBanned = activeBan != null,
                BanReason = activeBan?.Reason ?? string.Empty,
                BanDate = activeBan?.BannedAt ?? DateTime.MinValue,
                DetectionCount = detections.Count,
                HighestConfidenceDetection = detections.FirstOrDefault()?.ConfidenceScore ?? 0,
                RecentDetections = detections.Select(d => new DetectionRecord
                {
                    Type = d.DetectionType.ToString(),
                    Confidence = d.ConfidenceScore,
                    Evidence = d.Evidence,
                    DetectedAt = d.DetectedAt
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting player profile for {pubgUID}");
            return new PUBGPlayerProfile { IsValid = false };
        }
    }

    public async Task<bool> ReportCheatDetectionAsync(string pubgUID, PUBGCheatReport report)
    {
        try
        {
            var player = await _db.Players.FirstOrDefaultAsync(p => p.PlayerUID == pubgUID);
            if (player == null)
            {
                player = new Player 
                { 
                    PlayerUID = pubgUID, 
                    PlayerName = report.PlayerName ?? pubgUID 
                };
                _db.Players.Add(player);
                await _db.SaveChangesAsync();
            }

            // Create cheat detection record
            var detection = new CheatDetection
            {
                PlayerId = player.Id,
                DetectionType = report.DetectionType,
                ConfidenceScore = report.ConfidenceScore,
                Evidence = report.Evidence,
                DetectedAt = DateTime.UtcNow,
                AdditionalData = report.AdditionalData
            };

            _db.CheatDetections.Add(detection);
            await _db.SaveChangesAsync();

            _logger.LogWarning($"🚨 PUBG Cheat Detected: {pubgUID} | Type: {report.DetectionType} | Confidence: {report.ConfidenceScore:P}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reporting cheat detection for {pubgUID}");
            return false;
        }
    }

    public async Task<bool> BanPlayerImmediateAsync(string pubgUID, PUBGBanReason reason)
    {
        try
        {
            var player = await _db.Players.FirstOrDefaultAsync(p => p.PlayerUID == pubgUID);
            if (player == null)
                return false;

            // Check if already banned
            var existingBan = await _db.PlayerBans
                .FirstOrDefaultAsync(b => b.PlayerId == player.Id && b.UnbannedAt == null);

            if (existingBan != null)
            {
                _logger.LogInformation($"Player already banned: {pubgUID}");
                return true;
            }

            // Create ban record
            var ban = new PlayerBan
            {
                PlayerId = player.Id,
                Reason = reason.Reason,
                BanType = reason.IsPermanent ? BanType.PermanentBan : BanType.TemporaryBan,
                BannedAt = DateTime.UtcNow,
                IsPermanent = reason.IsPermanent,
                DurationDays = reason.DurationDays,
                IsAutomatic = reason.IsAutomatic,
                AdminNotes = $"PUBG Ban | Cheat Type: {reason.CheatType} | Evidence: {reason.Evidence}"
            };

            _db.PlayerBans.Add(ban);
            player.Status = PlayerStatus.Banned;
            await _db.SaveChangesAsync();

            _logger.LogCritical($"🚫 PUBG PLAYER BANNED: {pubgUID} | Reason: {reason.Reason}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error banning PUBG player {pubgUID}");
            return false;
        }
    }

    public static List<string> GetKnownCheatSignatures() => KnownCheatSignatures;

    public static Dictionary<string, PUBGTelemetryRequirement> GetPUBGTelemetryRequirements() 
        => PUBGRequiredTelemetry;
}

// Models
public class PUBGTelemetryRequirement
{
    public int RequiredFrequency { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class PUBGPlayerProfile
{
    public bool IsValid { get; set; }
    public string PUBGUID { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsBanned { get; set; }
    public string BanReason { get; set; } = string.Empty;
    public DateTime BanDate { get; set; }
    public int DetectionCount { get; set; }
    public double HighestConfidenceDetection { get; set; }
    public List<DetectionRecord> RecentDetections { get; set; } = new();
}

public class DetectionRecord
{
    public string Type { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Evidence { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}

public class PUBGCheatReport
{
    public string? PlayerName { get; set; }
    public DetectionType DetectionType { get; set; }
    public double ConfidenceScore { get; set; }
    public string Evidence { get; set; } = string.Empty;
    public string? AdditionalData { get; set; }
}

public class PUBGBanReason
{
    public string Reason { get; set; } = string.Empty;
    public string CheatType { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public bool IsPermanent { get; set; } = true;
    public int DurationDays { get; set; } = 0;
    public bool IsAutomatic { get; set; } = true;
}
