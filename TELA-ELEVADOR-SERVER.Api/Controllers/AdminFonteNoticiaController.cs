using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "PredioMatchesSlug")]
[Route("api/{slug}/admin/fonte-noticia")]
public sealed class AdminFonteNoticiaController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AdminFonteNoticiaController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetFontes([FromRoute] string slug)
    {
        var predio = await GetPredioAsync(slug);
        if (predio is null)
        {
            return NotFound(new { message = "Predio nao encontrado." });
        }

        if (!HasAccessToPredio(predio.Id))
        {
            return Forbid();
        }

        var preferencias = await _dbContext.PreferenciasNoticia
            .AsNoTracking()
            .Where(p => p.PredioId == predio.Id)
            .ToListAsync();

        var hasPreferencias = preferencias.Count > 0;
        var preferenciasMap = preferencias.ToDictionary(p => p.FonteNoticiaId, p => p.Habilitado);

        var fontes = await _dbContext.FontesNoticia
            .AsNoTracking()
            .OrderBy(f => f.Nome)
            .Select(f => new
            {
                f.Id,
                f.Chave,
                f.Nome,
                f.UrlBase,
                f.Ativo,
                f.CriadoEm,
                Habilitado = hasPreferencias
                    ? preferenciasMap.GetValueOrDefault(f.Id, false)
                    : f.Ativo
            })
            .ToListAsync();

        return Ok(fontes);
    }

    private async Task<Predio?> GetPredioAsync(string slug)
    {
        return await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);
    }

    private bool HasAccessToPredio(int predioId)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (string.Equals(role, "Developer", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var claim = User.FindFirst("predioId")?.Value;
        return int.TryParse(claim, out var claimPredioId) && claimPredioId == predioId;
    }
}
