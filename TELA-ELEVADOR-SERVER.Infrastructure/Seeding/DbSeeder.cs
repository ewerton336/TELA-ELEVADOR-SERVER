using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;
using TELA_ELEVADOR_SERVER.Infrastructure.Security;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Seeding;

public sealed class DbSeeder : IDbSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public DbSeeder(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
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
        var predio = await _dbContext.Predios.SingleOrDefaultAsync(p => p.Slug == "gramado");
        if (predio is null)
        {
            predio = new Predio
            {
                Slug = "gramado",
                Nome = "Gramado",
                Cidade = "Gramado",
                OrientationMode = "auto",
                CriadoEm = DateTime.UtcNow
            };
            _dbContext.Predios.Add(predio);
            await _dbContext.SaveChangesAsync();
        }

        var sindicoExists = await _dbContext.Sindicos.AnyAsync(s => s.PredioId == predio.Id && s.Usuario == "admin");
        if (!sindicoExists)
        {
            var (hash, salt) = _passwordHasher.HashPassword("123456");
            var sindico = new Sindico
            {
                PredioId = predio.Id,
                Usuario = "admin",
                SenhaHash = hash,
                SenhaSalt = salt,
                Role = "Sindico",
                CriadoEm = DateTime.UtcNow
            };

            _dbContext.Sindicos.Add(sindico);
            await _dbContext.SaveChangesAsync();
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
