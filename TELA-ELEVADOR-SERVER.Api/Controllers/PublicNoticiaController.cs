using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Application.Noticias;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Route("api/{slug}/noticia")]
public sealed class PublicNoticiaController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly INoticiaService _noticiaService;

    public PublicNoticiaController(AppDbContext dbContext, INoticiaService noticiaService)
    {
        _dbContext = dbContext;
        _noticiaService = noticiaService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNoticias([FromRoute] string slug, [FromQuery] int take = 30)
    {
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null)
        {
            return NotFound(new { message = "Predio nao encontrado." });
        }

        var hasPreferencias = await _dbContext.PreferenciasNoticia
            .AsNoTracking()
            .AnyAsync(p => p.PredioId == predio.Id);

        var fontesQuery = _dbContext.FontesNoticia
            .AsNoTracking()
            .Where(f => f.Ativo);

        List<string> enabledChaves;

        if (hasPreferencias)
        {
            enabledChaves = await _dbContext.PreferenciasNoticia
                .AsNoTracking()
                .Where(p => p.PredioId == predio.Id && p.Habilitado)
                .Join(fontesQuery,
                    preferencia => preferencia.FonteNoticiaId,
                    fonte => fonte.Id,
                    (_, fonte) => fonte.Chave)
                .Distinct()
                .ToListAsync();
        }
        else
        {
            enabledChaves = await fontesQuery
                .Select(f => f.Chave)
                .Distinct()
                .ToListAsync();
        }

        var normalizedTake = Math.Clamp(take, 1, 50);
        var items = await _noticiaService.BuscarNoticiasAsync(enabledChaves, normalizedTake);

        return Ok(new
        {
            items,
            lastUpdated = DateTime.UtcNow,
            enabledSourceIds = enabledChaves
        });
    }
}
