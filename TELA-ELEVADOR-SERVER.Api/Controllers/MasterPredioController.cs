using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;
using TELA_ELEVADOR_SERVER.Infrastructure.Services;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "DeveloperOnly")]
[Route("api/admin/predio")]
public sealed class MasterPredioController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly CidadeService _cidadeService;

    public MasterPredioController(AppDbContext dbContext, CidadeService cidadeService)
    {
        _dbContext = dbContext;
        _cidadeService = cidadeService;
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
                p.OrientationMode,
                p.CriadoEm
            })
            .ToListAsync();

        return Ok(predios);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePredio([FromBody] PredioRequest request)
    {
        var slugExists = await _dbContext.Predios
            .AnyAsync(p => p.Slug == request.Slug);

        if (slugExists)
        {
            return BadRequest(new { message = "Slug ja cadastrado." });
        }

        // Criar ou obter a cidade
        var cidade = await _cidadeService.GetOrCreateCidadeNormalizedAsync(request.Cidade);

        var predio = new Predio
        {
            Slug = request.Slug,
            Nome = request.Nome,
            CidadeId = cidade.Id,
            CriadoEm = DateTime.UtcNow
        };

        _dbContext.Predios.Add(predio);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPredios), new { predio.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePredio([FromRoute] int id, [FromBody] PredioRequest request)
    {
        var predio = await _dbContext.Predios.SingleOrDefaultAsync(p => p.Id == id);
        if (predio is null)
        {
            return NotFound(new { message = "Predio nao encontrado." });
        }

        var slugExists = await _dbContext.Predios
            .AnyAsync(p => p.Id != id && p.Slug == request.Slug);
        if (slugExists)
        {
            return BadRequest(new { message = "Slug ja cadastrado." });
        }

        // Criar ou obter a cidade
        var cidade = await _cidadeService.GetOrCreateCidadeNormalizedAsync(request.Cidade);

        predio.Slug = request.Slug;
        predio.Nome = request.Nome;
        predio.CidadeId = cidade.Id;

        await _dbContext.SaveChangesAsync();
        return Ok(new { predio.Id });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePredio([FromRoute] int id)
    {
        var predio = await _dbContext.Predios.SingleOrDefaultAsync(p => p.Id == id);
        if (predio is null)
        {
            return NotFound(new { message = "Predio nao encontrado." });
        }

        _dbContext.Predios.Remove(predio);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    public sealed record PredioRequest(string Slug, string Nome, string Cidade);
}
