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
        if (string.IsNullOrWhiteSpace(request.ConnectionId))
            return BadRequest(new { message = "connectionId é obrigatório." });

        await _hub.Clients.Client(request.ConnectionId)
            .SendAsync("ForceRefresh");

        return Ok(new { message = "Comando de atualização enviado." });
    }
}

public sealed record ForceRefreshRequest(string ConnectionId);
