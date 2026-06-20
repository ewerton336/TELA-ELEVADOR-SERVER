using System.Text.Json;
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
    private readonly ScreenDetailsService _details;
    private readonly IHubContext<PredioHub> _hub;

    public MasterMonitorController(
        ScreenMonitorService monitor,
        ScreenshotService screenshots,
        ScreenDetailsService details,
        IHubContext<PredioHub> hub)
    {
        _monitor = monitor;
        _screenshots = screenshots;
        _details = details;
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

        var target = _monitor.RequestScreenshot(request.DeviceId);

        if (!target.Found)
            return NotFound(new { message = "Tela não encontrada." });

        if (target.Connected && target.ConnectionId is not null)
        {
            await _hub.Clients.Client(target.ConnectionId).SendAsync("RequestScreenshot");
            return Ok(new { message = "Captura solicitada.", queued = false });
        }

        // Tela offline — captura fica agendada para a próxima reconexão.
        return Ok(new { message = "Tela offline — print agendado para a próxima reconexão.", queued = true });
    }

    [HttpPost("request-details")]
    public async Task<IActionResult> RequestDetails([FromBody] ForceRefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { message = "deviceId é obrigatório." });

        var target = _monitor.RequestDetails(request.DeviceId);

        if (!target.Found)
            return NotFound(new { message = "Tela não encontrada." });

        if (target.Connected && target.ConnectionId is not null)
        {
            await _hub.Clients.Client(target.ConnectionId).SendAsync("RequestScreenDetails");
            return Ok(new { message = "Detalhes solicitados.", queued = false });
        }

        // Tela offline — coleta fica agendada para a próxima reconexão.
        return Ok(new { message = "Tela offline — detalhes agendados para a próxima reconexão.", queued = true });
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

    [AllowAnonymous]
    [HttpPost("details-data")]
    public async Task<IActionResult> UploadDetails([FromBody] UploadDetailsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId)
            || request.Details.ValueKind == JsonValueKind.Undefined
            || request.Details.ValueKind == JsonValueKind.Null)
        {
            return BadRequest(new { message = "deviceId e detalhes são obrigatórios." });
        }

        _details.Save(request.DeviceId, request.Details.GetRawText());

        await _hub.Clients.Group("monitor")
            .SendAsync("ScreenDetailsReady", new { request.DeviceId, CapturedAt = DateTime.UtcNow });

        return Ok();
    }

    [HttpGet("details/{deviceId}")]
    public IActionResult GetDetails([FromRoute] string deviceId)
    {
        var entry = _details.Get(deviceId);
        if (entry is null)
            return NotFound();

        // Envelope: { "capturedAt": "...", "details": { ... } } — embute o JSON cru.
        var envelope = $"{{\"capturedAt\":{JsonSerializer.Serialize(entry.CapturedAt)},\"details\":{entry.Json}}}";
        return Content(envelope, "application/json");
    }

    /// <summary>
    /// Data/hora do último print e dos últimos detalhes de cada tela, para o
    /// monitor exibir o "último obtido" mesmo que não estivesse aberto quando
    /// a captura chegou.
    /// </summary>
    [HttpGet("captures")]
    public IActionResult GetCaptures()
    {
        var shots = _screenshots.GetTimestamps();
        var details = _details.GetTimestamps();

        var deviceIds = shots.Keys.Union(details.Keys);
        var result = deviceIds.Select(id => new
        {
            deviceId = id,
            screenshotAt = shots.TryGetValue(id, out var s) ? (DateTime?)s : null,
            detailsAt = details.TryGetValue(id, out var d) ? (DateTime?)d : null
        });

        return Ok(result);
    }
}

public sealed record ForceRefreshRequest(string DeviceId);

public sealed record UploadScreenshotRequest(string DeviceId, string ImageBase64);

public sealed record UploadDetailsRequest(string DeviceId, JsonElement Details);
