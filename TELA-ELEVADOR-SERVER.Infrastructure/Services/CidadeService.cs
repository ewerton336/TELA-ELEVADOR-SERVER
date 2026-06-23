using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;
using TELA_ELEVADOR_SERVER.Infrastructure.Utilities;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Services;

public sealed class CidadeService
{
    private readonly AppDbContext _dbContext;
    private readonly IGeocodingService _geocodingService;

    public CidadeService(AppDbContext dbContext, IGeocodingService geocodingService)
    {
        _dbContext = dbContext;
        _geocodingService = geocodingService;
    }

    /// <summary>
    /// Busca ou cria uma cidade baseada no nome (normalizado)
    /// Se a cidade não existir, tenta obter coordenadas via geocoding simplificado
    /// </summary>
    public async Task<Cidade> GetOrCreateCidadeNormalizedAsync(string nomeCidade)
    {
        if (string.IsNullOrWhiteSpace(nomeCidade))
            throw new ArgumentException("Nome da cidade não pode ser vazio", nameof(nomeCidade));

        var nomeNormalizado = StringNormalizer.NormalizeForSearch(nomeCidade);

        var cidadeExistente = await _dbContext.Cidades
            .FirstOrDefaultAsync(c => c.Nome == nomeNormalizado);

        if (cidadeExistente != null)
        {
            if (CoordenadasInvalidas(cidadeExistente.Latitude, cidadeExistente.Longitude))
            {
                var (lat, lon) = await ResolverCoordenadasAsync(nomeCidade);
                if (!CoordenadasInvalidas(lat, lon))
                {
                    cidadeExistente.Latitude = lat;
                    cidadeExistente.Longitude = lon;
                    await _dbContext.SaveChangesAsync();
                }
            }

            return cidadeExistente;
        }

        var (latitude, longitude) = await ResolverCoordenadasAsync(nomeCidade);

        var novaCidade = new Cidade
        {
            Nome = nomeNormalizado,
            NomeExibicao = nomeCidade.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            CriadoEm = DateTime.UtcNow
        };

        _dbContext.Cidades.Add(novaCidade);
        await _dbContext.SaveChangesAsync();

        return novaCidade;
    }

    private static bool CoordenadasInvalidas(double latitude, double longitude)
        => latitude == 0 && longitude == 0;

    private async Task<(double Latitude, double Longitude)> ResolverCoordenadasAsync(string nomeCidade)
    {
        try
        {
            var geo = await _geocodingService.GeocodeAsync(nomeCidade);
            if (geo is not null && !CoordenadasInvalidas(geo.Latitude, geo.Longitude))
                return (geo.Latitude, geo.Longitude);
        }
        catch
        {
        }

        return GetCoordinatesForCity(nomeCidade) ?? (0, 0);
    }

    /// <summary>
    /// Obtém coordenadas de uma cidade baseado em um mapeamento hardcoded
    /// Pode ser expandido para usar geocoding API no future
    /// </summary>
    private static (double latitude, double longitude)? GetCoordinatesForCity(string nomeCidade)
    {
        var nomeNormalizado = StringNormalizer.NormalizeForSearch(nomeCidade);

        var coordenadas = new Dictionary<string, (double, double)>
        {
            { "gramado", (-29.3789, -50.8744) },
            { "gramado, rs", (-29.3789, -50.8744) },
            { "praia grande", (-24.0058, -46.4028) },
            { "praia grande, sp", (-24.0058, -46.4028) },
            { "marilia", (-22.2139, -49.9458) },
            { "marilia, sp", (-22.2139, -49.9458) },
            { "sao paulo", (-23.5505, -46.6333) },
            { "sao paulo, sp", (-23.5505, -46.6333) },
            { "rio de janeiro", (-22.9068, -43.1729) },
            { "rio de janeiro, rj", (-22.9068, -43.1729) },
            { "belo horizonte", (-19.9167, -43.9345) },
            { "belo horizonte, mg", (-19.9167, -43.9345) },
        };

        if (coordenadas.TryGetValue(nomeNormalizado, out var coords))
            return coords;

        return null;
    }

    /// <summary>
    /// Obtém as coordenadas de uma cidade pelo ID
    /// </summary>
    public async Task<(double Latitude, double Longitude)> GetCoordinatesByCidadeIdAsync(int cidadeId)
    {
        var cidade = await _dbContext.Cidades.FindAsync(cidadeId);
        if (cidade == null)
            throw new InvalidOperationException($"Cidade com ID {cidadeId} não encontrada");

        return (cidade.Latitude, cidade.Longitude);
    }

    /// <summary>
    /// Busca uma cidade pelo nome (normalizado) SEM criar se não existir
    /// </summary>
    public async Task<Cidade?> BuscarCidadeNormalizedAsync(string nomeCidade)
    {
        if (string.IsNullOrWhiteSpace(nomeCidade))
            return null;

        var nomeNormalizado = StringNormalizer.NormalizeForSearch(nomeCidade);

        return await _dbContext.Cidades
            .FirstOrDefaultAsync(c => c.Nome == nomeNormalizado);
    }

    /// <summary>
    /// Obtém uma cidade pelo ID
    /// </summary>
    public async Task<Cidade?> GetCidadeByIdAsync(int cidadeId)
    {
        return await _dbContext.Cidades.FindAsync(cidadeId);
    }

    /// <summary>
    /// Lista todas as cidades
    /// </summary>
    public async Task<List<Cidade>> ListarTodasAsync()
    {
        return await _dbContext.Cidades
            .OrderBy(c => c.NomeExibicao)
            .ToListAsync();
    }
}
