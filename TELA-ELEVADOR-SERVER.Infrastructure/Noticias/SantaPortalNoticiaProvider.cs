using TELA_ELEVADOR_SERVER.Application.Noticias;
using Microsoft.Extensions.Configuration;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Noticias;

public sealed class SantaPortalNoticiaProvider : RssProviderBase, INoticiaProvider
{
    private const string FeedUrl = "https://santaportal.com.br/feed/";
    private readonly int _maxItensPorFonte;

    public SantaPortalNoticiaProvider(HttpClient httpClient, IConfiguration configuration) : base(httpClient)
    {
        _maxItensPorFonte = Math.Max(1, ParseIntOrDefault(configuration["NoticiasProviders:MaxItensPorFonte"], 10));
    }

    public SantaPortalNoticiaProvider(HttpClient httpClient) : base(httpClient)
    {
        _maxItensPorFonte = 10;
    }

    public string Chave => "SantaPortal";

    public async Task<List<NoticiaItem>> BuscarUltimasAsync()
    {
        var xml = await TryGetStringAsync(FeedUrl);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return new List<NoticiaItem>();
        }

        return Parse(xml, "Santa Portal", "https://placehold.co/800x450/1a6b3c/ffffff?text=Santa+Portal");
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

            items.Add(BuildItem(title, description, link, thumbnail, pubDate, source, null));
        }

        return items;
    }

    private static int ParseIntOrDefault(string? rawValue, int defaultValue)
    {
        return int.TryParse(rawValue, out var parsed) ? parsed : defaultValue;
    }
}
