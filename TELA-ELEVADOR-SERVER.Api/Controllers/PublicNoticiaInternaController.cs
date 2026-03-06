using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Route("api/{slug}/noticia-interna")]
public sealed class PublicNoticiaInternaController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public PublicNoticiaInternaController(AppDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetNoticiasInternas([FromRoute] string slug)
    {
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (predio is null)
        {
            return NotFound(new { message = "Predio nao encontrado." });
        }

        var agora = DateTime.UtcNow;
        var mediaBasePath = _configuration.GetValue<string>("MediaStorage:PublicUrl") ?? "/api/media";

        var noticias = await _dbContext.NoticiasInternas
            .AsNoTracking()
            .Where(n => n.PredioId == predio.Id)
            .Where(n => n.Ativo)
            .Where(n => (!n.InicioEm.HasValue || n.InicioEm <= agora)
                     && (!n.FimEm.HasValue || n.FimEm >= agora))
            .OrderByDescending(n => n.CriadoEm)
            .Select(n => new
            {
                n.Id,
                n.Titulo,
                n.Subtitulo,
                n.TipoMidia,
                MediaUrl = $"{mediaBasePath}/{n.NomeArquivo}",
                n.CriadoEm
            })
            .ToListAsync();

        return Ok(noticias);
    }
}
