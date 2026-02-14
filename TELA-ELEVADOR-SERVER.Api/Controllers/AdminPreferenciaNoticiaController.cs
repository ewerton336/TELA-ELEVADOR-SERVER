using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "PredioMatchesSlug")]
[Route("api/{slug}/admin/preferencia-noticia")]
public sealed class AdminPreferenciaNoticiaController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AdminPreferenciaNoticiaController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPut]
    public async Task<IActionResult> UpdatePreferencias(
        [FromRoute] string slug,
        [FromBody] PreferenciaNoticiaRequest request)
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

        var fontes = await _dbContext.FontesNoticia
            .AsNoTracking()
            .ToListAsync();

        var fonteMap = fontes.ToDictionary(f => f.Chave, StringComparer.OrdinalIgnoreCase);
        var invalid = request.Fontes
            .Where(f => !fonteMap.ContainsKey(f.Chave))
            .Select(f => f.Chave)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (invalid.Count > 0)
        {
            return BadRequest(new { message = "Fontes invalidas.", invalid });
        }

        var preferencias = await _dbContext.PreferenciasNoticia
            .Where(p => p.PredioId == predio.Id)
            .ToListAsync();

        var requestedFonteIds = new HashSet<int>();
        foreach (var fonteRequest in request.Fontes)
        {
            var fonte = fonteMap[fonteRequest.Chave];
            requestedFonteIds.Add(fonte.Id);
            var preferencia = preferencias.SingleOrDefault(p => p.FonteNoticiaId == fonte.Id);
            if (preferencia is null)
            {
                preferencias.Add(new PreferenciaNoticia
                {
                    PredioId = predio.Id,
                    FonteNoticiaId = fonte.Id,
                    Habilitado = fonteRequest.Habilitado,
                    CriadoEm = DateTime.UtcNow
                });
            }
            else
            {
                preferencia.Habilitado = fonteRequest.Habilitado;
            }
        }

        var toRemove = preferencias
            .Where(p => !requestedFonteIds.Contains(p.FonteNoticiaId))
            .ToList();

        _dbContext.PreferenciasNoticia.RemoveRange(toRemove);
        await _dbContext.SaveChangesAsync();

        return Ok(new { updated = request.Fontes.Count });
    }

    private async Task<Predio?> GetPredioAsync(string slug)
    {
        return await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);
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

    public sealed record PreferenciaNoticiaRequest(List<PreferenciaFonteInput> Fontes);
    public sealed record PreferenciaFonteInput(string Chave, bool Habilitado);
}
