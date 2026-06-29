using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Route("api/predios")]
public sealed class PublicPrediosController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PublicPrediosController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetPredios()
    {
        var predios = await _dbContext.Predios
            .AsNoTracking()
            .Include(p => p.Cidade)
            .OrderBy(p => p.Nome)
            .Select(p => new
            {
                p.Id,
                p.Slug,
                p.Nome,
                cidade = p.Cidade != null ? p.Cidade.NomeExibicao : "Sem cidade",
            })
            .ToListAsync();

        return Ok(predios);
    }
}
