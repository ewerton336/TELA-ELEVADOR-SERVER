using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Persistence;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Predio>(entity =>
        {
            entity.ToTable("Predio");
            entity.HasIndex(p => p.Slug).IsUnique();
        });

        modelBuilder.Entity<Sindico>(entity =>
        {
            entity.ToTable("Sindico");
            entity.HasIndex(s => new { s.PredioId, s.Usuario }).IsUnique();
            entity.HasOne(s => s.Predio)
                .WithMany(p => p.Sindicos)
                .HasForeignKey(s => s.PredioId);
        });

        modelBuilder.Entity<Aviso>(entity =>
        {
            entity.ToTable("Aviso");
            entity.HasOne(a => a.Predio)
                .WithMany(p => p.Avisos)
                .HasForeignKey(a => a.PredioId);
        });

        modelBuilder.Entity<FonteNoticia>(entity =>
        {
            entity.ToTable("FonteNoticia");
            entity.HasIndex(f => f.Chave).IsUnique();
        });

        modelBuilder.Entity<PreferenciaNoticia>(entity =>
        {
            entity.ToTable("PreferenciaNoticia");
            entity.HasKey(p => new { p.PredioId, p.FonteNoticiaId });
            entity.HasOne(p => p.Predio)
                .WithMany(p => p.PreferenciasNoticia)
                .HasForeignKey(p => p.PredioId);
            entity.HasOne(p => p.FonteNoticia)
                .WithMany(f => f.PreferenciasNoticia)
                .HasForeignKey(p => p.FonteNoticiaId);
        });
    }
}
