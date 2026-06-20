using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
    public DbSet<TickerMensagem> TickerMensagens => Set<TickerMensagem>();
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

    /// <summary>
    /// As colunas de data são "timestamp with time zone" (Postgres), e o Npgsql
    /// exige DateTime com Kind=Utc para gravar nelas. Datas vindas de formulários
    /// (ex.: início/fim de avisos e notícias internas, de um input datetime-local)
    /// chegam como Kind=Unspecified e estouravam um 500 no SaveChanges.
    /// Este conversor global normaliza todo DateTime para UTC na escrita e marca
    /// como UTC na leitura.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }
}

internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Utc
                ? v
                : (v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc)),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

internal sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Utc
                    ? v.Value
                    : (v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)))
                : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }
}
