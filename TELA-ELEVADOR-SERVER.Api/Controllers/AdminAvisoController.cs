using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "PredioMatchesSlug")]
[Route("api/{slug}/admin/aviso")]
public sealed class AdminAvisoController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AdminAvisoController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAvisos([FromRoute] string slug)
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

        var avisos = await _dbContext.Avisos
            .AsNoTracking()
            .Where(a => a.PredioId == predio.Id)
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

    [HttpPost]
    public async Task<IActionResult> CreateAviso([FromRoute] string slug, [FromBody] AvisoRequest request)
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

        var aviso = new Aviso
        {
            PredioId = predio.Id,
            Titulo = request.Titulo,
            Mensagem = request.Mensagem,
            InicioEm = request.InicioEm,
            FimEm = request.FimEm,
            Ativo = request.Ativo,
            CriadoEm = DateTime.UtcNow
        };

        _dbContext.Avisos.Add(aviso);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAvisos), new { slug }, new
        {
            aviso.Id,
            aviso.Titulo,
            aviso.Mensagem,
            aviso.InicioEm,
            aviso.FimEm,
            aviso.Ativo,
            aviso.CriadoEm
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAviso(
        [FromRoute] string slug,
        [FromRoute] int id,
        [FromBody] AvisoRequest request)
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

        var aviso = await _dbContext.Avisos
            .SingleOrDefaultAsync(a => a.Id == id && a.PredioId == predio.Id);

        if (aviso is null)
        {
            return NotFound(new { message = "Aviso nao encontrado." });
        }

        aviso.Titulo = request.Titulo;
        aviso.Mensagem = request.Mensagem;
        aviso.InicioEm = request.InicioEm;
        aviso.FimEm = request.FimEm;
        aviso.Ativo = request.Ativo;

        await _dbContext.SaveChangesAsync();
        return Ok(new
        {
            aviso.Id,
            aviso.Titulo,
            aviso.Mensagem,
            aviso.InicioEm,
            aviso.FimEm,
            aviso.Ativo,
            aviso.CriadoEm
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAviso([FromRoute] string slug, [FromRoute] int id)
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

        var aviso = await _dbContext.Avisos
            .SingleOrDefaultAsync(a => a.Id == id && a.PredioId == predio.Id);

        if (aviso is null)
        {
            return NotFound(new { message = "Aviso nao encontrado." });
        }

        _dbContext.Avisos.Remove(aviso);
        await _dbContext.SaveChangesAsync();

        return NoContent();
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

    public sealed record AvisoRequest(
        string Titulo,
        string Mensagem,
        DateTime? InicioEm,
        DateTime? FimEm,
        bool Ativo);
}
