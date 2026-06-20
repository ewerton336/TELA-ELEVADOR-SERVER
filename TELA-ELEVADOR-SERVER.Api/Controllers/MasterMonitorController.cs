using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TELA_ELEVADOR_SERVER.Api.Hubs;
using TELA_ELEVADOR_SERVER.Api.Services;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "DeveloperOnly")]
[Route("api/admin/monitor")]
public sealed class MasterMonitorController : ControllerBase
{
    private readonly ScreenMonitorService _monitor;
    private readonly ScreenshotService _screenshots;
    private readonly IHubContext<PredioHub> _hub;

    public MasterMonitorController(ScreenMonitorService monitor, ScreenshotService screenshots, IHubContext<PredioHub> hub)
    {
        _monitor = monitor;
        _screenshots = screenshots;
        _hub = hub;
    }

    [HttpGet("screens")]
    public IActionResult GetScreens()
    {
        return Ok(_monitor.GetAll());
    }

    [HttpPost("force-refresh")]
    public async Task<IActionResult> ForceRefresh([FromBody] ForceRefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { message = "deviceId é obrigatório." });

        var target = _monitor.RequestRefresh(request.DeviceId);

        if (!target.Found)
            return NotFound(new { message = "Tela não encontrada." });

        if (target.Connected && target.ConnectionId is not null)
        {
            await _hub.Clients.Client(target.ConnectionId)
                .SendAsync("ForceRefresh");

            return Ok(new { message = "Comando de atualização enviado.", queued = false });
        }

        // Tela offline — comando fica agendado para a próxima reconexão.
        return Ok(new { message = "Tela offline — atualização agendada para a próxima reconexão.", queued = true });
    }

    [HttpPost("request-screenshot")]
    public async Task<IActionResult> RequestScreenshot([FromBody] ForceRefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { message = "deviceId é obrigatório." });

        var connectionId = _monitor.GetActiveConnectionId(request.DeviceId);
        if (connectionId is null)
            return Conflict(new { message = "Tela offline — não é possível capturar agora." });

        await _hub.Clients.Client(connectionId).SendAsync("RequestScreenshot");
        return Ok(new { message = "Captura solicitada." });
    }

    [AllowAnonymous]
    [HttpPost("screenshot-data")]
    public async Task<IActionResult> UploadScreenshot([FromBody] UploadScreenshotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.ImageBase64))
            return BadRequest(new { message = "deviceId e imagem são obrigatórios." });

        var base64 = request.ImageBase64;
        var comma = base64.IndexOf(',');
        if (comma >= 0)
            base64 = base64[(comma + 1)..];

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch
        {
            return BadRequest(new { message = "Imagem inválida." });
        }

        _screenshots.Save(request.DeviceId, bytes, "image/png");

        await _hub.Clients.Group("monitor")
            .SendAsync("ScreenshotReady", new { request.DeviceId, CapturedAt = DateTime.UtcNow });

        return Ok();
    }

    [HttpGet("screenshot/{deviceId}")]
    public IActionResult GetScreenshot([FromRoute] string deviceId)
    {
        var shot = _screenshots.Get(deviceId);
        if (shot is null)
            return NotFound();

        return File(shot.Bytes, shot.ContentType);
    }
}

public sealed record ForceRefreshRequest(string DeviceId);

public sealed record UploadScreenshotRequest(string DeviceId, string ImageBase64);
