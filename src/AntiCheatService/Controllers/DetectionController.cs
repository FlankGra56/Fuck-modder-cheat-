using Microsoft.AspNetCore.Mvc;
using AntiCheatService.Core.Detection;

namespace AntiCheatService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DetectionController : ControllerBase
{
    private readonly IDetectionEngine _detectionEngine;
    private readonly ITelemetryAnalyzer _telemetryAnalyzer;
    private readonly ILogger<DetectionController> _logger;

    public DetectionController(
        IDetectionEngine detectionEngine,
        ITelemetryAnalyzer telemetryAnalyzer,
        ILogger<DetectionController> logger)
    {
        _detectionEngine = detectionEngine;
        _telemetryAnalyzer = telemetryAnalyzer;
        _logger = logger;
    }

    [HttpPost("analyze/{playerId}")]
    public async Task<IActionResult> AnalyzePlayer(string playerId)
    {
        _logger.LogInformation($"Analyzing: {playerId}");
        var result = await _detectionEngine.RunFullDetectionAsync(playerId);
        return Ok(result);
    }

    [HttpPost("report-telemetry")]
    public async Task<IActionResult> ReportTelemetry([FromBody] TelemetryReportRequest request)
    {
        await _telemetryAnalyzer.RecordTelemetryEventAsync(request.PlayerId, request.EventName);
        return Ok();
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

public class TelemetryReportRequest
{
    public string PlayerId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
}
