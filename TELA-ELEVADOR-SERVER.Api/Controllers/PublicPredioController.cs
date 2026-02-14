using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Route("api/{slug}/predio")]
public sealed class PublicPredioController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PublicPredioController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetPredio([FromRoute] string slug)
    {
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null)
        {
            return NotFound(new { message = "Predio nao encontrado." });
        }

        return Ok(new
        {
            predio.Id,
            predio.Slug,
            predio.Nome,
            predio.Cidade,
            predio.CriadoEm
        });
    }
}
