namespace AntiCheatService.Core.Detection;

public interface ITelemetryAnalyzer
{
    Task<TelemetryAnalysisResult> AnalyzeTelemetryGapAsync(string playerId, int timeWindowSeconds = 60);
    Task RecordTelemetryEventAsync(string playerId, string eventName, string? eventData = null);
}

public class TelemetryAnalysisResult
{
    public bool IsCheatDetected { get; set; }
    public double ConfidenceScore { get; set; }
    public List<MissingEvent> MissingEvents { get; set; } = new();
    public string Evidence { get; set; } = string.Empty;
}

public class MissingEvent
{
    public string EventName { get; set; } = string.Empty;
    public int ExpectedCount { get; set; }
    public int ReceivedCount { get; set; }
    public double ConfidencePercentage { get; set; }
}
