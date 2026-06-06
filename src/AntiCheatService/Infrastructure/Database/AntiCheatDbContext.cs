using Microsoft.EntityFrameworkCore;
using AntiCheatService.Core.Models;

namespace AntiCheatService.Infrastructure.Database;

public class AntiCheatDbContext : DbContext
{
    public AntiCheatDbContext(DbContextOptions<AntiCheatDbContext> options) : base(options) { }

    public DbSet<Player> Players { get; set; }
    public DbSet<TelemetryEvent> TelemetryEvents { get; set; }
    public DbSet<CheatDetection> CheatDetections { get; set; }
    public DbSet<PlayerBan> PlayerBans { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Player>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Player>()
            .HasIndex(p => p.PlayerUID).IsUnique();

        modelBuilder.Entity<TelemetryEvent>()
            .HasIndex(e => new { e.PlayerId, e.Timestamp });

        modelBuilder.Entity<PlayerBan>()
            .HasIndex(b => new { b.PlayerId, b.BannedAt });

        modelBuilder.Entity<CheatDetection>()
            .HasIndex(d => new { d.PlayerId, d.DetectedAt });
    }
}
