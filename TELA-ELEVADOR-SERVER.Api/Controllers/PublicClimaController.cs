using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Route("api/{slug}/clima")]
public sealed class PublicClimaController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PublicClimaController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetClima([FromRoute] string slug)
    {
        // Buscar o prédio pelo slug
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .Include(p => p.Cidade)
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null || predio.Cidade is null)
        {
            return NotFound(new { message = "Prédio ou cidade não encontrado(a)." });
        }

        // Buscar as previsões climáticas dos próximos 7 dias
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var fim = hoje.AddDays(7);

        var previsoes = await _dbContext.ClimaPrevisoesData
            .AsNoTracking()
            .Where(cp => cp.CidadeId == predio.CidadeId && cp.Data >= hoje && cp.Data <= fim)
            .OrderBy(cp => cp.Data)
            .ToListAsync();

        if (!previsoes.Any())
        {
            return Ok(new
            {
                location = predio.Cidade.NomeExibicao,
                days = new List<object>(),
                lastUpdated = DateTime.UtcNow
            });
        }

        // Mapear para resposta
        var dias = previsoes.Select(p => new
        {
            date = p.Data.ToDateTime(TimeOnly.MinValue),
            dateFormatted = p.Data.ToString("dd/MM/yyyy"),
            dayName = GetDayName(p.Data),
            temperatureMax = p.TemperaturaMaxima,
            temperatureMin = p.TemperaturaMinima,
            weatherCode = p.CodigoWmo,
            weatherDescription = p.Descricao,
            weatherIcon = p.Icone
        }).ToList();

        return Ok(new
        {
            location = predio.Cidade.NomeExibicao,
            days = dias,
            lastUpdated = previsoes.Max(p => p.AtualizadoEm)
        });
    }

    private static string GetDayName(DateOnly data)
    {
        var diasSemana = new[] { "Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado" };
        var dateTime = data.ToDateTime(TimeOnly.MinValue);
        return diasSemana[(int)dateTime.DayOfWeek];
    }
}
