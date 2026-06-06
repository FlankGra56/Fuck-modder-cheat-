using Microsoft.AspNetCore.Mvc;
using AntiCheatService.Core.Models;
using AntiCheatService.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace AntiCheatService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BanController : ControllerBase
{
    private readonly AntiCheatDbContext _db;
    private readonly ILogger<BanController> _logger;

    public BanController(AntiCheatDbContext db, ILogger<BanController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost("ban/{playerId}")]
    public async Task<IActionResult> BanPlayer(string playerId, [FromBody] BanRequest request)
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

            var ban = new PlayerBan
            {
                PlayerId = player.Id,
                Reason = request.Reason,
                BanType = BanType.PermanentBan,
                BannedAt = DateTime.UtcNow,
                IsPermanent = true,
                IsAutomatic = true
            };

            _db.PlayerBans.Add(ban);
            player.Status = PlayerStatus.Banned;
            await _db.SaveChangesAsync();

            _logger.LogWarning($"🚫 BANNED: {playerId}");
            return Ok(new { message = "Banned", banId = ban.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("status/{playerId}")]
    public async Task<IActionResult> GetBanStatus(string playerId)
    {
        var player = await _db.Players.FirstOrDefaultAsync(p => p.PlayerUID == playerId);
        if (player == null) return NotFound();

        var ban = await _db.PlayerBans
            .Where(b => b.PlayerId == player.Id && b.UnbannedAt == null)
            .OrderByDescending(b => b.BannedAt)
            .FirstOrDefaultAsync();

        return ban != null ? Ok(ban) : NotFound();
    }
}

public class BanRequest
{
    public string Reason { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
}
