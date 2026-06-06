namespace AntiCheatService.Core.Models;

public class PlayerBan
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public BanType BanType { get; set; }
    public DateTime BannedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnbannedAt { get; set; }
    public bool IsPermanent { get; set; }
    public bool IsAutomatic { get; set; }
    public Player? Player { get; set; }
}

public enum BanType
{
    TemporaryBan,
    PermanentBan,
    Suspension,
    AccountLocked
}
