using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Route("api/{slug}/aviso")]
public sealed class PublicAvisoController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PublicAvisoController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAvisos([FromRoute] string slug)
    {
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null)
        {
            return NotFound(new { message = "Predio nao encontrado." });
        }

        var agora = DateTime.UtcNow;

        var avisos = await _dbContext.Avisos
            .AsNoTracking()
            .Where(a => a.PredioId == predio.Id)
            .Where(a => a.Ativo)
            .Where(a => (!a.InicioEm.HasValue || a.InicioEm <= agora)
                && (!a.FimEm.HasValue || a.FimEm >= agora))
            .OrderByDescending(a => a.CriadoEm)
            .Select(a => new
            {
                a.Id,
                a.Titulo,
                a.Mensagem,
                a.InicioEm,
                a.FimEm,
                a.Ativo,
                a.CriadoEm
            })
            .ToListAsync();

        return Ok(avisos);
    }
}
