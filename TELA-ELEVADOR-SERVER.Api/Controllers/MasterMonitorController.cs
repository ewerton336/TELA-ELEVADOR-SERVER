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
    private readonly IHubContext<PredioHub> _hub;

    public MasterMonitorController(ScreenMonitorService monitor, IHubContext<PredioHub> hub)
    {
        _monitor = monitor;
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
}

public sealed record ForceRefreshRequest(string DeviceId);
