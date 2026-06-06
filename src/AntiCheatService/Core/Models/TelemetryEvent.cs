namespace AntiCheatService.Core.Models;

public class TelemetryEvent
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Player? Player { get; set; }
}
