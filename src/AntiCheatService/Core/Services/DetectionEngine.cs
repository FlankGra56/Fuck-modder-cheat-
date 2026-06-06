using AntiCheatService.Infrastructure.Database;

namespace AntiCheatService.Core.Detection;

public class DetectionEngine : IDetectionEngine
{
    private readonly ITelemetryAnalyzer _telemetryAnalyzer;
    private readonly ILogger<DetectionEngine> _logger;

    public DetectionEngine(ITelemetryAnalyzer telemetryAnalyzer, ILogger<DetectionEngine> logger)
    {
        _telemetryAnalyzer = telemetryAnalyzer;
        _logger = logger;
    }

    public async Task<DetectionEngineResult> RunFullDetectionAsync(string playerId)
    {
        var result = new DetectionEngineResult();
        var telemetryResult = await _telemetryAnalyzer.AnalyzeTelemetryGapAsync(playerId);

        if (telemetryResult.IsCheatDetected)
        {
            result.DetectionLayers.Add(new DetectionLayerResult
            {
                LayerName = "Telemetry Gap Analysis",
                Detected = true,
                ConfidenceScore = telemetryResult.ConfidenceScore,
                Evidence = telemetryResult.Evidence
            });
            result.OverallConfidenceScore = telemetryResult.ConfidenceScore;
            result.IsCheatDetected = true;
            result.RecommendedAction = "INSTANT_BAN";
        }

        return result;
    }

    public async Task<DetectionEngineResult> RunTelemetryDetectionAsync(string playerId)
    {
        return await RunFullDetectionAsync(playerId);
    }
}
