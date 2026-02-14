using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;
using TELA_ELEVADOR_SERVER.Infrastructure.Security;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "DeveloperOnly")]
[Route("api/admin/sindico")]
public sealed class MasterSindicoController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public MasterSindicoController(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<IActionResult> GetSindicos([FromQuery] int? predioId)
    {
        var query = _dbContext.Sindicos
            .AsNoTracking()
            .Where(s => s.Role == "Sindico");

        if (predioId.HasValue)
        {
            query = query.Where(s => s.PredioId == predioId.Value);
        }

        var list = await query
            .OrderBy(s => s.Usuario)
            .Select(s => new
            {
                s.Id,
                s.PredioId,
                s.Usuario,
                s.Role,
                s.CriadoEm
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSindico([FromBody] SindicoRequest request)
    {
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == request.PredioId);

        if (predio is null)
        {
            return BadRequest(new { message = "Predio nao encontrado." });
        }

        var exists = await _dbContext.Sindicos
            .AnyAsync(s => s.PredioId == request.PredioId && s.Usuario == request.Usuario);

        if (exists)
        {
            return BadRequest(new { message = "Usuario ja cadastrado para este predio." });
        }

        var (hash, salt) = _passwordHasher.HashPassword(request.Senha);
        var sindico = new Sindico
        {
            PredioId = request.PredioId,
            Usuario = request.Usuario,
            SenhaHash = hash,
            SenhaSalt = salt,
            Role = "Sindico",
            CriadoEm = DateTime.UtcNow
        };

        _dbContext.Sindicos.Add(sindico);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSindicos), new { sindico.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSindico([FromRoute] int id, [FromBody] SindicoUpdateRequest request)
    {
        var sindico = await _dbContext.Sindicos.SingleOrDefaultAsync(s => s.Id == id);
        if (sindico is null)
        {
            return NotFound(new { message = "Sindico nao encontrado." });
        }

        if (sindico.Role == "Developer")
        {
            return BadRequest(new { message = "Nao e permitido alterar usuario developer." });
        }

        if (!string.IsNullOrWhiteSpace(request.Usuario))
        {
            var exists = await _dbContext.Sindicos
                .AnyAsync(s => s.Id != id && s.PredioId == sindico.PredioId && s.Usuario == request.Usuario);
            if (exists)
            {
                return BadRequest(new { message = "Usuario ja cadastrado para este predio." });
            }

            sindico.Usuario = request.Usuario;
        }

        if (!string.IsNullOrWhiteSpace(request.Senha))
        {
            var (hash, salt) = _passwordHasher.HashPassword(request.Senha);
            sindico.SenhaHash = hash;
            sindico.SenhaSalt = salt;
        }

        await _dbContext.SaveChangesAsync();
        return Ok(new { sindico.Id });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSindico([FromRoute] int id)
    {
        var sindico = await _dbContext.Sindicos.SingleOrDefaultAsync(s => s.Id == id);
        if (sindico is null)
        {
            return NotFound(new { message = "Sindico nao encontrado." });
        }

        if (sindico.Role == "Developer")
        {
            return BadRequest(new { message = "Nao e permitido remover usuario developer." });
        }

        _dbContext.Sindicos.Remove(sindico);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    public sealed record SindicoRequest(int PredioId, string Usuario, string Senha);
    public sealed record SindicoUpdateRequest(string? Usuario, string? Senha);
}
