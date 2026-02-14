using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "DeveloperOnly")]
[Route("api/admin/predio")]
public sealed class MasterPredioController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public MasterPredioController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetPredios()
    {
        var predios = await _dbContext.Predios
            .AsNoTracking()
            .OrderBy(p => p.Nome)
            .Select(p => new
            {
                p.Id,
                p.Slug,
                p.Nome,
                p.Cidade,
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

        var predio = new Predio
        {
            Slug = request.Slug,
            Nome = request.Nome,
            Cidade = request.Cidade,
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

        predio.Slug = request.Slug;
        predio.Nome = request.Nome;
        predio.Cidade = request.Cidade;

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
