using Microsoft.AspNetCore.Mvc;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;
using TELA_ELEVADOR_SERVER.Infrastructure.Services;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Route("api/admin/cidades")]
public sealed class AdminCidadeController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly CidadeService _cidadeService;

    public AdminCidadeController(AppDbContext dbContext, CidadeService cidadeService)
    {
        _dbContext = dbContext;
        _cidadeService = cidadeService;
    }

    /// <summary>
    /// Lista todas as cidades de São Paulo (sem autenticação)
    /// Utilizado pelo frontend para popular seletor de cidades ao cadastrar prédios
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListarCidades()
    {
        var cidades = await _cidadeService.ListarTodasAsync();

        return Ok(cidades.Select(c => new
        {
            c.Id,
            c.Nome,
            c.NomeExibicao,
            c.Latitude,
            c.Longitude,
            c.CriadoEm
        }));
    }
}
