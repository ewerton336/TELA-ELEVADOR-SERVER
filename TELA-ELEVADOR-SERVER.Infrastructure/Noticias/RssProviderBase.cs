using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TELA_ELEVADOR_SERVER.Application.Noticias;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Noticias;

public abstract class RssProviderBase
{
    private static readonly Regex HtmlRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex ImgRegex = new(@"<img[^>]+src=[""']([^""']+)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SizeRegex = new(@"-\d{2,4}x\d{2,4}(?=\.[a-zA-Z0-9]+$)", RegexOptions.Compiled);

    protected RssProviderBase(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    protected HttpClient HttpClient { get; }

    protected async Task<string?> TryGetStringAsync(string url)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            request.Headers.Accept.ParseAdd("application/rss+xml, application/xml, text/xml, */*");
            request.Headers.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");

            using var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }
    }

    protected static IEnumerable<XElement> ReadItems(string xml)
    {
        var doc = XDocument.Parse(xml);
        return doc.Descendants().Where(e => e.Name.LocalName == "item");
    }

    protected static string? ReadElementValue(XElement item, string localName)
    {
        return item.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value.Trim();
    }

    protected static string ReadDescription(XElement item)
    {
        var encoded = item.Elements().FirstOrDefault(e => e.Name.LocalName == "encoded")?.Value;
        var description = ReadElementValue(item, "description");
        return WebUtility.HtmlDecode(encoded ?? description ?? string.Empty);
    }

    protected static string ExtractFirstParagraph(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var match = Regex.Match(html, "<p[^>]*>([\\s\\S]*?)</p>", RegexOptions.IgnoreCase);
        var value = match.Success ? match.Groups[1].Value : html;
        return CleanHtml(value);
    }

    protected static string CleanHtml(string html)
    {
        return HtmlRegex.Replace(WebUtility.HtmlDecode(html ?? string.Empty), " ")
            .Replace("\u00A0", " ")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Trim();
    }

    protected static string ExtractFirstImage(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var match = ImgRegex.Match(html);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    protected static string? GetEnclosureUrl(XElement item)
    {
        return item.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "enclosure")
            ?.Attribute("url")
            ?.Value;
    }

    protected static string? GetMediaUrl(XElement item)
    {
        return item.Elements()
            .FirstOrDefault(e => e.Name.LocalName is "content" or "thumbnail")
            ?.Attribute("url")
            ?.Value;
    }

    protected static string UpgradeImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        var builder = new UriBuilder(uri);
        builder.Path = SizeRegex.Replace(builder.Path, string.Empty);

        var query = ParseQuery(builder.Query);
        var changed = false;

        if (query.ContainsKey("w"))
        {
            query["w"] = "1600";
            changed = true;
        }
        if (query.ContainsKey("width"))
        {
            query["width"] = "1600";
            changed = true;
        }
        if (query.ContainsKey("h"))
        {
            query["h"] = "900";
            changed = true;
        }
        if (query.ContainsKey("height"))
        {
            query["height"] = "900";
            changed = true;
        }
        if (query.ContainsKey("resize"))
        {
            query["resize"] = "1600,900";
            changed = true;
        }
        if (query.ContainsKey("fit"))
        {
            query["fit"] = "1600,900";
            changed = true;
        }

        if (changed)
        {
            builder.Query = BuildQuery(query);
        }

        return builder.Uri.ToString();
    }

    protected static string GenerateId(string title)
    {
        try
        {
            var trimmed = Uri.EscapeDataString(title).Length > 30
                ? Uri.EscapeDataString(title)[..30]
                : Uri.EscapeDataString(title);
            var bytes = System.Text.Encoding.UTF8.GetBytes(trimmed);
            var base64 = Convert.ToBase64String(bytes);
            var clean = new string(base64.Where(char.IsLetterOrDigit).ToArray());
            return clean.Length > 16 ? clean[..16] : clean;
        }
        catch
        {
            return Guid.NewGuid().ToString("N")[..16];
        }
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        var trimmed = query.StartsWith('?') ? query[1..] : query;
        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            var key = Uri.UnescapeDataString(pieces[0]);
            var value = pieces.Length > 1 ? Uri.UnescapeDataString(pieces[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    private static string BuildQuery(Dictionary<string, string> query)
    {
        return string.Join("&", query.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
    }

    protected static string FormatRelativeDate(string dateStr)
    {
        if (!DateTime.TryParse(dateStr, out var date))
        {
            return "Hoje";
        }

        var now = DateTime.Now;
        var diff = now - date.ToLocalTime();
        var diffHours = (int)Math.Floor(diff.TotalHours);
        var diffDays = (int)Math.Floor(diff.TotalDays);

        if (diffHours < 1) return "Agora";
        if (diffHours < 24) return $"{diffHours}h atras";
        if (diffDays == 1) return "Ontem";
        if (diffDays < 7) return $"{diffDays} dias atras";

        return date.ToString("dd MMM", new CultureInfo("pt-BR"));
    }

    protected static NoticiaItem BuildItem(
        string title,
        string description,
        string link,
        string thumbnail,
        string pubDate,
        string source,
        string? category)
    {
        return new NoticiaItem(
            GenerateId(title),
            title,
            description,
            link,
            thumbnail,
            pubDate,
            FormatRelativeDate(pubDate),
            source,
            category);
    }
}
