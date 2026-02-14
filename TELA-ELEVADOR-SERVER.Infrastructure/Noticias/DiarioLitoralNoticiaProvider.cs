using TELA_ELEVADOR_SERVER.Application.Noticias;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Noticias;

public sealed class DiarioLitoralNoticiaProvider : RssProviderBase, INoticiaProvider
{
    private const string FeedUrl = "https://www.diariodolitoral.com.br/praia-grande/rss/";

    public DiarioLitoralNoticiaProvider(HttpClient httpClient) : base(httpClient)
    {
    }

    public string Chave => "DiarioDoLitoral";

    public async Task<List<NoticiaItem>> BuscarUltimasAsync()
    {
        var xml = await TryGetStringAsync(FeedUrl);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return new List<NoticiaItem>();
        }

        return Parse(xml, "Diario do Litoral", "https://placehold.co/800x450/0066cc/ffffff?text=Diario+Litoral");
    }

    private List<NoticiaItem> Parse(string xml, string source, string placeholder)
    {
        var items = new List<NoticiaItem>();

        foreach (var item in ReadItems(xml).Take(15))
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
            var description = TrimToNextPunctuation(ExtractFirstParagraph(rawDescription), 200);

            items.Add(BuildItem(title, description, link, thumbnail, pubDate, source, null));
        }

        return items;
    }
}
