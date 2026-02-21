using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;
using TELA_ELEVADOR_SERVER.Infrastructure.Noticias;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "DeveloperOnly")]
[Route("api/admin/noticia")]
public sealed class AdminNoticiaHealthCheckController : ControllerBase
{
    private readonly NoticiaBackgroundWorker _worker;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminNoticiaHealthCheckController> _logger;

    public AdminNoticiaHealthCheckController(
        NoticiaBackgroundWorker worker,
        AppDbContext dbContext,
        ILogger<AdminNoticiaHealthCheckController> logger)
    {
        _worker = worker;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("healthcheck")]
    public async Task<IActionResult> Healthcheck([FromBody] HealthcheckRequest? request)
    {
        try
        {
            var chavesProviders = request?.FonteChave != null
                ? new List<string> { request.FonteChave }
                : null;

            var fontesCarregadas = await _worker.BuscarNoticiasDeProvidersAsync(chavesProviders);
            var total = fontesCarregadas.Values.Sum();

            _logger.LogInformation(
                "Healthcheck executado: {Total} noticias novas. Detalhes: {Fontes}",
                total,
                string.Join(", ", fontesCarregadas.Select(kv => $"{kv.Key}:{kv.Value}"))
            );

            return Ok(new
            {
                success = true,
                fontesCarregadas,
                total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar healthcheck de noticias");
            return StatusCode(500, new
            {
                success = false,
                message = "Erro ao forcar carregamento de noticias",
                error = ex.Message
            });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await _dbContext.Noticias
                .AsNoTracking()
                .GroupBy(n => n.FonteChave)
                .Select(g => new { Fonte = g.Key, Count = g.Count() })
                .ToListAsync();

            var resultado = stats.ToDictionary(s => s.Fonte, s => s.Count);

            // Garantir que todas as fontes apareçam, mesmo com 0 notícias
            var fontesConhecidas = new[] { "G1", "SantaPortal", "DiarioDoLitoral" };
            foreach (var fonte in fontesConhecidas)
            {
                if (!resultado.ContainsKey(fonte))
                {
                    resultado[fonte] = 0;
                }
            }

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatisticas de noticias");
            return StatusCode(500, new
            {
                message = "Erro ao buscar estatisticas",
                error = ex.Message
            });
        }
    }
}

public sealed record HealthcheckRequest(string? FonteChave);
