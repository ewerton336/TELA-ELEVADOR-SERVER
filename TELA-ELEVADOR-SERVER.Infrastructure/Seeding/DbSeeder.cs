using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;
using TELA_ELEVADOR_SERVER.Infrastructure.Security;
using TELA_ELEVADOR_SERVER.Infrastructure.Services;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Seeding;

public sealed class DbSeeder : IDbSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly CidadeService _cidadeService;

    public DbSeeder(AppDbContext dbContext, IPasswordHasher passwordHasher, CidadeService cidadeService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _cidadeService = cidadeService;
    }

    public async Task SeedAsync()
    {
        await SeedFontesNoticiaAsync();
        await SeedPredioESindicoAsync();
        await SeedDeveloperAsync();
    }

    private async Task SeedFontesNoticiaAsync()
    {
        var fontes = new List<FonteNoticia>
        {
            new()
            {
                Chave = "G1",
                Nome = "G1",
                UrlBase = "https://g1.globo.com",
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            },
            new()
            {
                Chave = "SantaPortal",
                Nome = "SantaPortal",
                UrlBase = "https://santaportal.com.br",
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            },
            new()
            {
                Chave = "DiarioDoLitoral",
                Nome = "Diario do Litoral",
                UrlBase = "https://www.diariodolitoral.com.br",
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            }
        };

        foreach (var fonte in fontes)
        {
            var exists = await _dbContext.FontesNoticia.AnyAsync(f => f.Chave == fonte.Chave);
            if (!exists)
            {
                _dbContext.FontesNoticia.Add(fonte);
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedPredioESindicoAsync()
    {
        // Mapear prédios com suas cidades
        var prediosCidades = new Dictionary<string, (string nome, string cidade)>
        {
            { "gramado", ("Residencial Gramado IX", "Praia Grande") },
            { "marilia", ("Edificio Marilia", "Marília") }
        };

        foreach (var (slug, (nome, cidadeNome)) in prediosCidades)
        {
            var predio = await _dbContext.Predios.SingleOrDefaultAsync(p => p.Slug == slug);

            if (predio is null)
            {
                var cidade = await _cidadeService.BuscarCidadeNormalizedAsync(cidadeNome);
                if (cidade is null)
                {
                    continue; // Pular se cidade não existir
                }

                predio = new Predio
                {
                    Slug = slug,
                    Nome = nome,
                    CidadeId = cidade.Id,
                    OrientationMode = "auto",
                    CriadoEm = DateTime.UtcNow
                };
                _dbContext.Predios.Add(predio);
            }
            else
            {
                // Sempre atualizar nome e cidade para prédios existentes
                var cidade = await _cidadeService.BuscarCidadeNormalizedAsync(cidadeNome);
                if (cidade is not null)
                {
                    predio.Nome = nome;
                    predio.CidadeId = cidade.Id;
                }
            }

            await _dbContext.SaveChangesAsync();

            // Criar sindico se não existir
            var sindicoExists = await _dbContext.Sindicos.AnyAsync(s => s.PredioId == predio.Id && s.Usuario == slug);
            if (!sindicoExists)
            {
                // Senha = slug do prédio (ex: predio "marilia" tem senha "marilia")
                var (hash, salt) = _passwordHasher.HashPassword(slug);
                var sindico = new Sindico
                {
                    PredioId = predio.Id,
                    Usuario = slug,
                    SenhaHash = hash,
                    SenhaSalt = salt,
                    Role = "Sindico",
                    CriadoEm = DateTime.UtcNow
                };

                _dbContext.Sindicos.Add(sindico);
                await _dbContext.SaveChangesAsync();
            }
        }
    }

    private async Task SeedDeveloperAsync()
    {
        var exists = await _dbContext.Sindicos
            .AnyAsync(s => s.Usuario == "ewerton" && s.Role == "Developer");

        if (exists)
        {
            return;
        }

        var (hash, salt) = _passwordHasher.HashPassword("123123");
        var developer = new Sindico
        {
            PredioId = null,
            Usuario = "ewerton",
            SenhaHash = hash,
            SenhaSalt = salt,
            Role = "Developer",
            CriadoEm = DateTime.UtcNow
        };

        _dbContext.Sindicos.Add(developer);
        await _dbContext.SaveChangesAsync();
    }
}
