using TELA_ELEVADOR_SERVER.Application.Noticias;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Noticias;

public sealed class NoticiaService : INoticiaService
{
    private readonly Dictionary<string, INoticiaProvider> _providers;

    public NoticiaService(IEnumerable<INoticiaProvider> providers)
    {
        _providers = providers
            .GroupBy(p => p.Chave, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(p => p.Chave, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<List<NoticiaItem>> BuscarNoticiasAsync(IEnumerable<string> chaves)
    {
        var selected = chaves
            .Where(chave => _providers.ContainsKey(chave))
            .Select(chave => _providers[chave])
            .Distinct()
            .ToList();

        var tasks = selected.Select(async provider =>
        {
            try
            {
                return await provider.BuscarUltimasAsync();
            }
            catch
            {
                return new List<NoticiaItem>();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(items => items)
            .OrderByDescending(item => ParseDate(item.PubDate))
            .ToList();
    }

    private static DateTime ParseDate(string value)
    {
        return DateTime.TryParse(value, out var parsed)
            ? parsed.ToUniversalTime()
            : DateTime.MinValue;
    }
}
