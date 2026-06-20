using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Api.Hubs;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "PredioMatchesSlug")]
[Route("api/{slug}/admin/ticker")]
public sealed class AdminTickerController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<PredioHub> _hub;

    public AdminTickerController(AppDbContext dbContext, IHubContext<PredioHub> hub)
    {
        _dbContext = dbContext;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> GetTickerMensagens([FromRoute] string slug)
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

        var mensagens = await _dbContext.TickerMensagens
            .AsNoTracking()
            .Where(t => t.PredioId == predio.Id)
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

    [HttpPost]
    public async Task<IActionResult> CreateTickerMensagem([FromRoute] string slug, [FromBody] TickerMensagemRequest request)
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

        var mensagem = new TickerMensagem
        {
            PredioId = predio.Id,
            Texto = request.Texto,
            Ativo = request.Ativo,
            Ordem = request.Ordem ?? 0,
            CriadoEm = DateTime.UtcNow
        };

        _dbContext.TickerMensagens.Add(mensagem);
        await _dbContext.SaveChangesAsync();

        await NotifyTickerChangedAsync(slug);

        return CreatedAtAction(nameof(GetTickerMensagens), new { slug }, new
        {
            mensagem.Id,
            mensagem.Texto,
            mensagem.Ativo,
            mensagem.Ordem,
            mensagem.CriadoEm
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTickerMensagem(
        [FromRoute] string slug,
        [FromRoute] int id,
        [FromBody] TickerMensagemRequest request)
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

        var mensagem = await _dbContext.TickerMensagens
            .SingleOrDefaultAsync(t => t.Id == id && t.PredioId == predio.Id);

        if (mensagem is null)
        {
            return NotFound(new { message = "Mensagem nao encontrada." });
        }

        mensagem.Texto = request.Texto;
        mensagem.Ativo = request.Ativo;
        mensagem.Ordem = request.Ordem ?? 0;

        await _dbContext.SaveChangesAsync();

        await NotifyTickerChangedAsync(slug);

        return Ok(new
        {
            mensagem.Id,
            mensagem.Texto,
            mensagem.Ativo,
            mensagem.Ordem,
            mensagem.CriadoEm
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTickerMensagem([FromRoute] string slug, [FromRoute] int id)
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

        var mensagem = await _dbContext.TickerMensagens
            .SingleOrDefaultAsync(t => t.Id == id && t.PredioId == predio.Id);

        if (mensagem is null)
        {
            return NotFound(new { message = "Mensagem nao encontrada." });
        }

        _dbContext.TickerMensagens.Remove(mensagem);
        await _dbContext.SaveChangesAsync();

        await NotifyTickerChangedAsync(slug);

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
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.Equals(role, "Developer", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var claim = User.FindFirst("predioId")?.Value;
        return int.TryParse(claim, out var claimPredioId) && claimPredioId == predioId;
    }

    private async Task NotifyTickerChangedAsync(string slug)
    {
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null) return;

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

        await _hub.Clients.Group(slug).SendAsync("ReceiveTickerMensagens", mensagens);
    }

    public sealed record TickerMensagemRequest(
        string Texto,
        bool Ativo,
        int? Ordem);
}
