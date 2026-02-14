using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Configurations;

public sealed class PreferenciaNoticiaEfConfiguration : IEntityTypeConfiguration<PreferenciaNoticia>
{
    public void Configure(EntityTypeBuilder<PreferenciaNoticia> builder)
    {
        builder.ToTable("PreferenciaNoticia");
        builder.HasKey(p => new { p.PredioId, p.FonteNoticiaId });
        builder.HasOne(p => p.Predio)
            .WithMany(p => p.PreferenciasNoticia)
            .HasForeignKey(p => p.PredioId);
        builder.HasOne(p => p.FonteNoticia)
            .WithMany(f => f.PreferenciasNoticia)
            .HasForeignKey(p => p.FonteNoticiaId);
    }
}
