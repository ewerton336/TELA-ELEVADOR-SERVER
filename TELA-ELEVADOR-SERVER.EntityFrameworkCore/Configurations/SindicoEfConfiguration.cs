using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Configurations;

public sealed class SindicoEfConfiguration : IEntityTypeConfiguration<Sindico>
{
    public void Configure(EntityTypeBuilder<Sindico> builder)
    {
        builder.ToTable("Sindico");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseIdentityByDefaultColumn();
        builder.Property(s => s.Role).HasMaxLength(30).IsRequired();
        builder.HasIndex(s => new { s.PredioId, s.Usuario }).IsUnique();
        builder.HasOne(s => s.Predio)
            .WithMany(p => p.Sindicos)
            .HasForeignKey(s => s.PredioId);
    }
}
