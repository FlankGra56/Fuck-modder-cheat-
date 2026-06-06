namespace AntiCheatService.Core.Detection;

public interface IDetectionEngine
{
    Task<DetectionEngineResult> RunFullDetectionAsync(string playerId);
    Task<DetectionEngineResult> RunTelemetryDetectionAsync(string playerId);
}

public class DetectionEngineResult
{
    public bool IsCheatDetected { get; set; }
    public double OverallConfidenceScore { get; set; }
    public List<DetectionLayerResult> DetectionLayers { get; set; } = new();
    public string RecommendedAction { get; set; } = "MONITOR";
    public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
}

public class DetectionLayerResult
{
    public string LayerName { get; set; } = string.Empty;
    public bool Detected { get; set; }
    public double ConfidenceScore { get; set; }
    public string Evidence { get; set; } = string.Empty;
}
