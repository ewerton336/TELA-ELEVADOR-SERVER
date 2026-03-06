using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TELA_ELEVADOR_SERVER.Api.Services;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "DeveloperOnly")]
[Route("api/admin/monitor")]
public sealed class MasterMonitorController : ControllerBase
{
    private readonly ScreenMonitorService _monitor;

    public MasterMonitorController(ScreenMonitorService monitor)
    {
        _monitor = monitor;
    }

    [HttpGet("screens")]
    public IActionResult GetScreens()
    {
        return Ok(_monitor.GetAll());
    }
}
