using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Api.Hubs;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "PredioMatchesSlug")]
[Route("api/{slug}/admin/predio")]
public sealed class AdminPredioController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<PredioHub> _hub;

    public AdminPredioController(AppDbContext dbContext, IHubContext<PredioHub> hub)
    {
        _dbContext = dbContext;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrientation([FromRoute] string slug)
    {
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null)
        {
            return NotFound(new { message = "Predio nao encontrado." });
        }

        if (!HasAccessToPredio(predio.Id))
        {
            return Forbid();
        }

        return Ok(new { predio.OrientationMode });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateOrientation(
        [FromRoute] string slug,
        [FromBody] OrientationRequest request)
    {
        var predio = await _dbContext.Predios
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null)
        {
            return NotFound(new { message = "Predio nao encontrado." });
        }

        if (!HasAccessToPredio(predio.Id))
        {
            return Forbid();
        }

        var mode = request.OrientationMode?.Trim().ToLowerInvariant();
        if (mode is not "auto" and not "portrait" and not "landscape")
        {
            return BadRequest(new { message = "Orientacao invalida." });
        }

        predio.OrientationMode = mode;
        await _dbContext.SaveChangesAsync();

        await _hub.Clients.Group(slug).SendAsync("ReceiveOrientation", mode);

        return Ok(new { predio.OrientationMode });
    }

    [HttpGet("modulos")]
    public async Task<IActionResult> GetModules([FromRoute] string slug)
    {
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null)
            return NotFound(new { message = "Predio nao encontrado." });

        if (!HasAccessToPredio(predio.Id))
            return Forbid();

        return Ok(new ScreenModulesResponse(
            predio.ModuloBuildingNotice,
            predio.ModuloWeather,
            predio.ModuloHeadlineNews,
            predio.ModuloNewsTicker));
    }

    [HttpPut("modulos")]
    public async Task<IActionResult> UpdateModules(
        [FromRoute] string slug,
        [FromBody] ScreenModulesRequest request)
    {
        var predio = await _dbContext.Predios
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null)
            return NotFound(new { message = "Predio nao encontrado." });

        if (!HasAccessToPredio(predio.Id))
            return Forbid();

        predio.ModuloBuildingNotice = request.BuildingNotice;
        predio.ModuloWeather = request.Weather;
        predio.ModuloHeadlineNews = request.HeadlineNews;
        predio.ModuloNewsTicker = request.NewsTicker;
        await _dbContext.SaveChangesAsync();

        var modules = new ScreenModulesResponse(
            predio.ModuloBuildingNotice,
            predio.ModuloWeather,
            predio.ModuloHeadlineNews,
            predio.ModuloNewsTicker);

        await _hub.Clients.Group(slug).SendAsync("ReceiveModules", modules);

        return Ok(modules);
    }

    private bool HasAccessToPredio(int predioId)
    {
        var role = User.FindFirst("role")?.Value;
        if (string.Equals(role, "Developer", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var claim = User.FindFirst("predioId")?.Value;
        return int.TryParse(claim, out var claimPredioId) && claimPredioId == predioId;
    }

    public sealed record OrientationRequest(string OrientationMode);
    public sealed record ScreenModulesRequest(bool BuildingNotice, bool Weather, bool HeadlineNews, bool NewsTicker);
    public sealed record ScreenModulesResponse(bool BuildingNotice, bool Weather, bool HeadlineNews, bool NewsTicker);
}
