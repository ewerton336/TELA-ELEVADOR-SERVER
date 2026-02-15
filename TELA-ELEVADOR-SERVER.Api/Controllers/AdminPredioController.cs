using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "PredioMatchesSlug")]
[Route("api/{slug}/admin/predio")]
public sealed class AdminPredioController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AdminPredioController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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

        if (!IsDeveloper())
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

        if (!IsDeveloper())
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

        return Ok(new { predio.OrientationMode });
    }

    private bool IsDeveloper()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        Console.WriteLine($"[AdminPredioController] Role claim (ClaimTypes.Role): '{role}'");
        var isDev = string.Equals(role, "Developer", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"[AdminPredioController] IsDeveloper result: {isDev}");
        return isDev;
    }

    public sealed record OrientationRequest(string OrientationMode);
}
