using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Route("api/{slug}/ticker")]
public sealed class PublicTickerController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PublicTickerController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetTickerMensagens([FromRoute] string slug)
    {
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null)
        {
            return NotFound(new { message = "Predio nao encontrado." });
        }

        var mensagens = await _dbContext.TickerMensagens
            .AsNoTracking()
            .Where(t => t.PredioId == predio.Id && t.Ativo)
            .OrderBy(t => t.Ordem)
            .ThenByDescending(t => t.CriadoEm)
            .Select(t => new
            {
                t.Id,
                t.Texto,
                t.Ativo,
                t.Ordem,
                t.CriadoEm
            })
            .ToListAsync();

        return Ok(mensagens);
    }
}
