using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TELA_ELEVADOR_SERVER.Application.Noticias;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Noticias;

public sealed class NoticiaBackgroundWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);
    private static readonly TimeSpan Retencao = TimeSpan.FromDays(7);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NoticiaBackgroundWorker> _logger;

    public NoticiaBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<NoticiaBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ExecutarCicloAsync(stoppingToken);

        using var timer = new PeriodicTimer(Intervalo);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExecutarCicloAsync(stoppingToken);
        }
    }

    private async Task ExecutarCicloAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var providers = scope.ServiceProvider.GetServices<INoticiaProvider>().ToList();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (providers.Count == 0)
            {
                _logger.LogWarning("Nenhum provider de noticias registrado.");
                return;
            }

            var fetched = await BuscarNoticiasAsync(providers, stoppingToken);
            var (novasCount, removidasCount) = await PersistirNoticiasAsync(dbContext, fetched, stoppingToken);

            _logger.LogInformation("Ciclo concluido: {NovasCount} novas, {RemovidasCount} removidas", novasCount, removidasCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar noticias.");
        }
    }

    /// <summary>
    /// Busca notícias de providers específicos ou de todos se não especificado.
    /// Retorna um dicionário com a chave da fonte e a quantidade de notícias novas persistidas.
    /// </summary>
    public async Task<Dictionary<string, int>> BuscarNoticiasDeProvidersAsync(List<string>? chavesProviders = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var todosProviders = scope.ServiceProvider.GetServices<INoticiaProvider>().ToList();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (todosProviders.Count == 0)
        {
            _logger.LogWarning("Nenhum provider de noticias registrado.");
            return new Dictionary<string, int>();
        }

        var providersParaBuscar = chavesProviders == null || chavesProviders.Count == 0
            ? todosProviders
            : todosProviders.Where(p => chavesProviders.Contains(p.Chave, StringComparer.OrdinalIgnoreCase)).ToList();

        if (providersParaBuscar.Count == 0)
        {
            _logger.LogWarning("Nenhum provider encontrado para as chaves especificadas.");
            return new Dictionary<string, int>();
        }

        var fetched = await BuscarNoticiasAsync(providersParaBuscar, cancellationToken);

        // Agrupar por fonte para contar quantas novas de cada
        var porFonte = fetched.GroupBy(f => f.FonteChave)
            .ToDictionary(g => g.Key, g => g.ToList());

        var resultado = new Dictionary<string, int>();

        foreach (var provider in providersParaBuscar)
        {
            var itemsDaFonte = porFonte.GetValueOrDefault(provider.Chave, new List<(NoticiaItem Item, string FonteChave)>());
            var (novasCount, _) = await PersistirNoticiasAsync(dbContext, itemsDaFonte, cancellationToken);
            resultado[provider.Chave] = novasCount;
        }

        _logger.LogInformation("Healthcheck concluido: {Total} novas no total", resultado.Values.Sum());

        return resultado;
    }

    private async Task<List<(NoticiaItem Item, string FonteChave)>> BuscarNoticiasAsync(
        IEnumerable<INoticiaProvider> providers,
        CancellationToken stoppingToken)
    {
        var tasks = providers.Select(async provider =>
        {
            try
            {
                var items = await provider.BuscarUltimasAsync();
                _logger.LogInformation("Provider {Provider} retornou {Count} noticias", provider.Chave, items.Count);
                return items.Select(item => (Item: item, FonteChave: provider.Chave)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao buscar noticias do provider {Provider}: {Message}", provider.Chave, ex.Message);
                return new List<(NoticiaItem Item, string FonteChave)>();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(items => items)
            .Where(item => !string.IsNullOrWhiteSpace(item.Item.Link))
            .ToList();
    }

    private async Task<(int NovasCount, int RemovidasCount)> PersistirNoticiasAsync(
        AppDbContext dbContext,
        List<(NoticiaItem Item, string FonteChave)> fetched,
        CancellationToken stoppingToken)
    {
        var agoraUtc = DateTime.UtcNow;
        var limiteUtc = agoraUtc.Subtract(Retencao);

        var links = fetched
            .Select(item => item.Item.Link)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existentes = links.Count == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                await dbContext.Noticias
                    .AsNoTracking()
                    .Where(n => links.Contains(n.Link))
                    .Select(n => n.Link)
                    .ToListAsync(stoppingToken),
                StringComparer.OrdinalIgnoreCase);

        var novas = fetched
            .Where(item => !existentes.Contains(item.Item.Link))
            .Select(item => MapToEntity(item.Item, item.FonteChave, agoraUtc))
            .ToList();

        if (novas.Count > 0)
        {
            dbContext.Noticias.AddRange(novas);
        }

        var antigas = await dbContext.Noticias
            .Where(n => n.PublicadoEmUtc < limiteUtc)
            .ToListAsync(stoppingToken);

        if (antigas.Count > 0)
        {
            dbContext.Noticias.RemoveRange(antigas);
        }

        if (novas.Count > 0 || antigas.Count > 0)
        {
            await dbContext.SaveChangesAsync(stoppingToken);
        }

        return (novas.Count, antigas.Count);
    }

    private static Noticia MapToEntity(NoticiaItem item, string fonteChave, DateTime agoraUtc)
    {
        var publicadoEmUtc = ParsePublishedUtc(item.PubDate, agoraUtc);

        return new Noticia
        {
            FonteChave = fonteChave,
            FonteNome = item.Source ?? fonteChave,
            Titulo = item.Title ?? string.Empty,
            Descricao = item.Description ?? string.Empty,
            Link = item.Link ?? string.Empty,
            ImagemUrl = item.Thumbnail ?? string.Empty,
            PubDateRaw = item.PubDate ?? string.Empty,
            PublicadoEmUtc = publicadoEmUtc,
            Categoria = item.Category,
            CriadoEm = agoraUtc
        };
    }

    private static DateTime ParsePublishedUtc(string? value, DateTime fallbackUtc)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallbackUtc;
        }

        return DateTime.TryParse(value, out var parsed)
            ? parsed.ToUniversalTime()
            : fallbackUtc;
    }
}
