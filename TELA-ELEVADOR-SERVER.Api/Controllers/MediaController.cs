using Microsoft.AspNetCore.Mvc;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Route("api/media")]
public sealed class MediaController : ControllerBase
{
    private readonly string _mediaBasePath;

    public MediaController(IConfiguration configuration)
    {
        _mediaBasePath = configuration.GetValue<string>("MediaStorage:BasePath") ?? "/app/media";
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
