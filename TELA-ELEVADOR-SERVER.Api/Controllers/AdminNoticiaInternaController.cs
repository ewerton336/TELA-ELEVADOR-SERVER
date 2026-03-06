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

    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    private static readonly HashSet<string> AllowedVideoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/webm"
    };

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
        [FromForm] string titulo,
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

        var contentType = arquivo.ContentType;
        string tipoMidia;

        if (AllowedImageTypes.Contains(contentType))
            tipoMidia = "imagem";
        else if (AllowedVideoTypes.Contains(contentType))
            tipoMidia = "video";
        else
            return BadRequest(new { message = "Tipo de arquivo não permitido. Use imagem (JPEG, PNG, GIF, WebP) ou vídeo (MP4, WebM)." });

        var ext = Path.GetExtension(arquivo.FileName);
        var nomeArquivo = $"{Guid.NewGuid()}{ext}";
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
        [FromForm] string titulo,
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

            var contentType = arquivo.ContentType;
            string tipoMidia;

            if (AllowedImageTypes.Contains(contentType))
                tipoMidia = "imagem";
            else if (AllowedVideoTypes.Contains(contentType))
                tipoMidia = "video";
            else
                return BadRequest(new { message = "Tipo de arquivo não permitido." });

            // Delete old file
            var oldFilePath = Path.Combine(_mediaBasePath, noticia.NomeArquivo);
            if (System.IO.File.Exists(oldFilePath))
                System.IO.File.Delete(oldFilePath);

            var ext = Path.GetExtension(arquivo.FileName);
            var nomeArquivo = $"{Guid.NewGuid()}{ext}";
            var newFilePath = Path.Combine(_mediaBasePath, nomeArquivo);

            await using (var stream = new FileStream(newFilePath, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            noticia.TipoMidia = tipoMidia;
            noticia.NomeArquivo = nomeArquivo;
            noticia.NomeArquivoOriginal = arquivo.FileName;
            noticia.ContentType = contentType;
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
