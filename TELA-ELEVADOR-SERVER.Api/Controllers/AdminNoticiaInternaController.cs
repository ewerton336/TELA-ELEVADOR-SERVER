using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Api.Hubs;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Authorize(Policy = "PredioMatchesSlug")]
[Route("api/{slug}/admin/noticia-interna")]
public sealed class AdminNoticiaInternaController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<PredioHub> _hub;
    private readonly string _mediaBasePath;
    private readonly string _mediaPublicUrl;
    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    internal readonly record struct MediaKind(string TipoMidia, string Extensao, string ContentType);

    // Content-Types aceitos. Inclui variantes não-padrão que celulares mandam
    // (image/jpg, image/pjpeg) para não rejeitar upload legítimo do usuário.
    private static readonly Dictionary<string, MediaKind> ContentTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"]  = new("imagem", ".jpg", "image/jpeg"),
        ["image/jpg"]   = new("imagem", ".jpg", "image/jpeg"),
        ["image/pjpeg"] = new("imagem", ".jpg", "image/jpeg"),
        ["image/png"]   = new("imagem", ".png", "image/png"),
        ["image/gif"]   = new("imagem", ".gif", "image/gif"),
        ["image/webp"]  = new("imagem", ".webp", "image/webp"),
        ["video/mp4"]   = new("video", ".mp4", "video/mp4"),
        ["video/webm"]  = new("video", ".webm", "video/webm"),
    };

    // Fallback por extensão quando o Content-Type do navegador é genérico
    // (application/octet-stream) ou ausente — comum em uploads mobile.
    private static readonly Dictionary<string, MediaKind> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"]  = new("imagem", ".jpg", "image/jpeg"),
        [".jpeg"] = new("imagem", ".jpg", "image/jpeg"),
        [".png"]  = new("imagem", ".png", "image/png"),
        [".gif"]  = new("imagem", ".gif", "image/gif"),
        [".webp"] = new("imagem", ".webp", "image/webp"),
        [".mp4"]  = new("video", ".mp4", "video/mp4"),
        [".webm"] = new("video", ".webm", "video/webm"),
    };

    // Resolve o tipo de mídia priorizando o Content-Type; se ele for genérico ou
    // desconhecido, cai para a extensão do arquivo. Sempre devolve uma extensão
    // canônica em minúsculas, garantindo que o MediaController sirva com o
    // Content-Type certo depois. Retorna null se nada casar.
    internal static MediaKind? ResolveMedia(IFormFile arquivo)
    {
        var contentType = arquivo.ContentType?.Trim();
        if (!string.IsNullOrEmpty(contentType) && ContentTypeMap.TryGetValue(contentType, out var byContentType))
            return byContentType;

        var ext = Path.GetExtension(arquivo.FileName);
        if (!string.IsNullOrEmpty(ext) && ExtensionMap.TryGetValue(ext, out var byExtension))
            return byExtension;

        return null;
    }

    public AdminNoticiaInternaController(
        AppDbContext dbContext,
        IHubContext<PredioHub> hub,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _hub = hub;
        _mediaBasePath = configuration.GetValue<string>("MediaStorage:BasePath") ?? "/app/media";
        _mediaPublicUrl = configuration.GetValue<string>("MediaStorage:PublicUrl") ?? "/api/media";
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromRoute] string slug)
    {
        var predio = await GetPredioAsync(slug);
        if (predio is null) return NotFound(new { message = "Predio nao encontrado." });
        if (!HasAccessToPredio(predio.Id)) return Forbid();

        var noticias = await _dbContext.NoticiasInternas
            .AsNoTracking()
            .Where(n => n.PredioId == predio.Id)
            .OrderByDescending(n => n.CriadoEm)
            .Select(n => new
            {
                n.Id,
                n.Titulo,
                n.Subtitulo,
                n.TipoMidia,
                MediaUrl = $"{_mediaPublicUrl}/{n.NomeArquivo}",
                n.NomeArquivoOriginal,
                n.InicioEm,
                n.FimEm,
                n.Ativo,
                n.CriadoEm
            })
            .ToListAsync();

        return Ok(noticias);
    }

    [HttpPost]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        [FromRoute] string slug,
        [FromForm] string? titulo,
        [FromForm] string? subtitulo,
        [FromForm] DateTime? inicioEm,
        [FromForm] DateTime? fimEm,
        IFormFile arquivo)
    {
        var predio = await GetPredioAsync(slug);
        if (predio is null) return NotFound(new { message = "Predio nao encontrado." });
        if (!HasAccessToPredio(predio.Id)) return Forbid();

        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { message = "Arquivo é obrigatório." });

        if (arquivo.Length > MaxFileSizeBytes)
            return BadRequest(new { message = "Arquivo excede o limite de 25 MB." });

        var media = ResolveMedia(arquivo);
        if (media is null)
            return BadRequest(new { message = "Formato não suportado. Envie uma imagem (JPEG, PNG, GIF, WebP) ou vídeo (MP4, WebM)." });

        var tipoMidia = media.Value.TipoMidia;
        var contentType = media.Value.ContentType;
        var nomeArquivo = $"{Guid.NewGuid()}{media.Value.Extensao}";
        var filePath = Path.Combine(_mediaBasePath, nomeArquivo);

        Directory.CreateDirectory(_mediaBasePath);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await arquivo.CopyToAsync(stream);
        }

        var noticia = new NoticiaInterna
        {
            PredioId = predio.Id,
            Titulo = titulo,
            Subtitulo = subtitulo,
            TipoMidia = tipoMidia,
            NomeArquivo = nomeArquivo,
            NomeArquivoOriginal = arquivo.FileName,
            ContentType = contentType,
            InicioEm = inicioEm,
            FimEm = fimEm,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };

        _dbContext.NoticiasInternas.Add(noticia);
        await _dbContext.SaveChangesAsync();

        await NotifyNoticiasInternasChangedAsync(slug);

        return CreatedAtAction(nameof(GetAll), new { slug }, new
        {
            noticia.Id,
            noticia.Titulo,
            noticia.Subtitulo,
            noticia.TipoMidia,
            MediaUrl = $"{_mediaPublicUrl}/{noticia.NomeArquivo}",
            noticia.NomeArquivoOriginal,
            noticia.InicioEm,
            noticia.FimEm,
            noticia.Ativo,
            noticia.CriadoEm
        });
    }

    [HttpPut("{id:int}")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Update(
        [FromRoute] string slug,
        [FromRoute] int id,
        [FromForm] string? titulo,
        [FromForm] string? subtitulo,
        [FromForm] DateTime? inicioEm,
        [FromForm] DateTime? fimEm,
        [FromForm] bool ativo,
        IFormFile? arquivo = null)
    {
        var predio = await GetPredioAsync(slug);
        if (predio is null) return NotFound(new { message = "Predio nao encontrado." });
        if (!HasAccessToPredio(predio.Id)) return Forbid();

        var noticia = await _dbContext.NoticiasInternas
            .SingleOrDefaultAsync(n => n.Id == id && n.PredioId == predio.Id);

        if (noticia is null)
            return NotFound(new { message = "Notícia interna não encontrada." });

        noticia.Titulo = titulo;
        noticia.Subtitulo = subtitulo;
        noticia.InicioEm = inicioEm;
        noticia.FimEm = fimEm;
        noticia.Ativo = ativo;

        // Replace file if a new one was uploaded
        if (arquivo is not null && arquivo.Length > 0)
        {
            if (arquivo.Length > MaxFileSizeBytes)
                return BadRequest(new { message = "Arquivo excede o limite de 25 MB." });

            var media = ResolveMedia(arquivo);
            if (media is null)
                return BadRequest(new { message = "Formato não suportado. Envie uma imagem (JPEG, PNG, GIF, WebP) ou vídeo (MP4, WebM)." });

            // Delete old file
            var oldFilePath = Path.Combine(_mediaBasePath, noticia.NomeArquivo);
            if (System.IO.File.Exists(oldFilePath))
                System.IO.File.Delete(oldFilePath);

            var nomeArquivo = $"{Guid.NewGuid()}{media.Value.Extensao}";
            var newFilePath = Path.Combine(_mediaBasePath, nomeArquivo);

            await using (var stream = new FileStream(newFilePath, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            noticia.TipoMidia = media.Value.TipoMidia;
            noticia.NomeArquivo = nomeArquivo;
            noticia.NomeArquivoOriginal = arquivo.FileName;
            noticia.ContentType = media.Value.ContentType;
        }

        await _dbContext.SaveChangesAsync();
        await NotifyNoticiasInternasChangedAsync(slug);

        return Ok(new
        {
            noticia.Id,
            noticia.Titulo,
            noticia.Subtitulo,
            noticia.TipoMidia,
            MediaUrl = $"{_mediaPublicUrl}/{noticia.NomeArquivo}",
            noticia.NomeArquivoOriginal,
            noticia.InicioEm,
            noticia.FimEm,
            noticia.Ativo,
            noticia.CriadoEm
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] string slug, [FromRoute] int id)
    {
        var predio = await GetPredioAsync(slug);
        if (predio is null) return NotFound(new { message = "Predio nao encontrado." });
        if (!HasAccessToPredio(predio.Id)) return Forbid();

        var noticia = await _dbContext.NoticiasInternas
            .SingleOrDefaultAsync(n => n.Id == id && n.PredioId == predio.Id);

        if (noticia is null)
            return NotFound(new { message = "Notícia interna não encontrada." });

        // Delete file from disk
        var filePath = Path.Combine(_mediaBasePath, noticia.NomeArquivo);
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        _dbContext.NoticiasInternas.Remove(noticia);
        await _dbContext.SaveChangesAsync();

        await NotifyNoticiasInternasChangedAsync(slug);

        return NoContent();
    }

    private async Task<Predio?> GetPredioAsync(string slug)
    {
        return await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);
    }

    private bool HasAccessToPredio(int predioId)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.Equals(role, "Developer", StringComparison.OrdinalIgnoreCase))
            return true;

        var claim = User.FindFirst("predioId")?.Value;
        return int.TryParse(claim, out var claimPredioId) && claimPredioId == predioId;
    }

    private async Task NotifyNoticiasInternasChangedAsync(string slug)
    {
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null) return;

        var agora = DateTime.UtcNow;
        var noticias = await _dbContext.NoticiasInternas
            .AsNoTracking()
            .Where(n => n.PredioId == predio.Id && n.Ativo)
            .Where(n => (!n.InicioEm.HasValue || n.InicioEm <= agora)
                     && (!n.FimEm.HasValue || n.FimEm >= agora))
            .OrderByDescending(n => n.CriadoEm)
            .Select(n => new
            {
                n.Id,
                n.Titulo,
                n.Subtitulo,
                n.TipoMidia,
                MediaUrl = $"{_mediaPublicUrl}/{n.NomeArquivo}",
                n.CriadoEm
            })
            .ToListAsync();

        await _hub.Clients.Group(slug)
            .SendAsync("NoticiasInternasChanged", noticias);
    }
}
