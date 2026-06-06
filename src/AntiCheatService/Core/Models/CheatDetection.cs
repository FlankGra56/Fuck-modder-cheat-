namespace AntiCheatService.Core.Models;

public class CheatDetection
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public DetectionType DetectionType { get; set; }
    public double ConfidenceScore { get; set; }
    public string Evidence { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public Player? Player { get; set; }
}

public enum DetectionType
{
    TelemetryGap,
    CallbackHijack,
    ModuleTampering,
    WallhackSuspected,
    BypassFlag,
    ImpossibleReactionTime
}
