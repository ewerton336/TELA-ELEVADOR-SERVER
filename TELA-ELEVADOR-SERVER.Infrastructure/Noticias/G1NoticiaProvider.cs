using TELA_ELEVADOR_SERVER.Application.Noticias;
using Microsoft.Extensions.Configuration;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Noticias;

public sealed class G1NoticiaProvider : RssProviderBase, INoticiaProvider
{
    private const string FeedUrl = "https://g1.globo.com/rss/g1/sp/santos-regiao/";
    private const string FallbackUrl = "https://news.google.com/rss/search?q=santos+OR+baixada+santista+site:g1.globo.com&hl=pt-BR&gl=BR&ceid=BR:pt-419";

    private readonly int _maxItensPorFonte;

    public G1NoticiaProvider(HttpClient httpClient, IConfiguration configuration) : base(httpClient)
    {
        _maxItensPorFonte = Math.Max(1, ParseIntOrDefault(configuration["NoticiasProviders:MaxItensPorFonte"], 10));
    }

    public G1NoticiaProvider(HttpClient httpClient) : base(httpClient)
    {
        _maxItensPorFonte = 10;
    }

    public string Chave => "G1";

    public async Task<List<NoticiaItem>> BuscarUltimasAsync()
    {
        var xml = await TryGetStringAsync(FeedUrl) ?? await TryGetStringAsync(FallbackUrl);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return new List<NoticiaItem>();
        }

        return Parse(xml, "G1", "https://placehold.co/800x450/c4170c/ffffff?text=G1+Santos");
    }

    private List<NoticiaItem> Parse(string xml, string source, string placeholder)
    {
        var items = new List<NoticiaItem>();

        foreach (var item in ReadItems(xml).Take(_maxItensPorFonte))
        {
            var title = ReadElementValue(item, "title") ?? string.Empty;
            var link = ReadElementValue(item, "link") ?? string.Empty;
            var pubDate = ReadElementValue(item, "pubDate") ?? string.Empty;
            var rawDescription = ReadDescription(item);

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
            {
                continue;
            }

            var thumbnail = GetEnclosureUrl(item)
                ?? GetMediaUrl(item)
                ?? ExtractFirstImage(rawDescription);

            thumbnail = string.IsNullOrWhiteSpace(thumbnail) ? placeholder : UpgradeImageUrl(thumbnail);
            var description = ExtractFirstParagraph(rawDescription);
            var category = ExtractCategoryFromUrl(link);

            items.Add(BuildItem(title, description, link, thumbnail, pubDate, source, category));
        }

        return items;
    }

    private static string ExtractCategoryFromUrl(string link)
    {
        var urlLower = link.ToLowerInvariant();

        if (urlLower.Contains("/praia-grande/")) return "Praia Grande";
        if (urlLower.Contains("/santos/")) return "Santos";
        if (urlLower.Contains("/guaruja/")) return "Guaruja";
        if (urlLower.Contains("/cubatao/")) return "Cubatao";
        if (urlLower.Contains("/sao-vicente/")) return "Sao Vicente";
        if (urlLower.Contains("/bertioga/")) return "Bertioga";
        if (urlLower.Contains("/mongagua/")) return "Mongagua";
        if (urlLower.Contains("/itanhaem/")) return "Itanhaem";
        if (urlLower.Contains("/peruibe/")) return "Peruibe";

        if (urlLower.Contains("/policia/") || urlLower.Contains("/crime/")) return "Policia";
        if (urlLower.Contains("/transito/")) return "Transito";
        if (urlLower.Contains("/economia/")) return "Economia";
        if (urlLower.Contains("/saude/")) return "Saude";
        if (urlLower.Contains("/educacao/")) return "Educacao";
        if (urlLower.Contains("/esporte/")) return "Esportes";
        if (urlLower.Contains("/politica/")) return "Politica";

        return "Baixada Santista";
    }

    private static int ParseIntOrDefault(string? rawValue, int defaultValue)
    {
        return int.TryParse(rawValue, out var parsed) ? parsed : defaultValue;
    }
}
