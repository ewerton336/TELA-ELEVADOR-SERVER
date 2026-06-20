using Microsoft.AspNetCore.Mvc;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Route("api/media")]
public sealed class MediaController : ControllerBase
{
    private readonly string _mediaBasePath;
    private readonly IHttpClientFactory _httpClientFactory;

    public MediaController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _mediaBasePath = configuration.GetValue<string>("MediaStorage:BasePath") ?? "/app/media";
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Proxy de imagens externas para o "print" da tela. O html2canvas não
    /// consegue capturar imagens cross-origin (ex.: fotos de notícias do G1),
    /// pois elas não enviam cabeçalhos CORS — então elas saem em branco no print.
    /// Este endpoint busca a imagem no servidor e a devolve same-origin, no
    /// formato que o html2canvas espera (bytes da imagem em 200).
    /// </summary>
    [HttpGet("proxy")]
    public async Task<IActionResult> Proxy([FromQuery] string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(new { message = "URL invalida." });
        }

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (TelaElevador image proxy)");

            using var response = await client.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "O conteudo nao e uma imagem." });
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return File(bytes, contentType);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Falha ao buscar a imagem." });
        }
    }

    [HttpGet("{fileName}")]
    public IActionResult GetMedia([FromRoute] string fileName)
    {
        // Reject path traversal attempts
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName.Contains("..", StringComparison.Ordinal)
            || fileName.Contains('/')
            || fileName.Contains('\\')
            || Path.GetFileName(fileName) != fileName)
        {
            return BadRequest(new { message = "Nome de arquivo invalido." });
        }

        var filePath = Path.Combine(_mediaBasePath, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "Arquivo nao encontrado." });
        }

        var contentType = GetContentType(fileName);
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, contentType, enableRangeProcessing: true);
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }
}
