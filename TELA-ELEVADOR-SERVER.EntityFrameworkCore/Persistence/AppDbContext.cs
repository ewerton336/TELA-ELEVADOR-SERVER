using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Predio> Predios => Set<Predio>();
    public DbSet<Sindico> Sindicos => Set<Sindico>();
    public DbSet<Aviso> Avisos => Set<Aviso>();
    public DbSet<FonteNoticia> FontesNoticia => Set<FonteNoticia>();
    public DbSet<PreferenciaNoticia> PreferenciasNoticia => Set<PreferenciaNoticia>();
    public DbSet<Noticia> Noticias => Set<Noticia>();
    public DbSet<NoticiaInterna> NoticiasInternas => Set<NoticiaInterna>();
    public DbSet<Cidade> Cidades => Set<Cidade>();
    public DbSet<ClimaPrevisao> ClimaPrevisoesData => Set<ClimaPrevisao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
