namespace AntiCheatService.Core.Models;

public class Player
{
    public Guid Id { get; set; }
    public string PlayerUID { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public PlayerStatus Status { get; set; } = PlayerStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    
    public ICollection<TelemetryEvent> TelemetryEvents { get; set; } = new List<TelemetryEvent>();
    public ICollection<CheatDetection> CheatDetections { get; set; } = new List<CheatDetection>();
    public ICollection<PlayerBan> Bans { get; set; } = new List<PlayerBan>();
}

public enum PlayerStatus
{
    Active,
    Suspended,
    Banned,
    WatchList
}
