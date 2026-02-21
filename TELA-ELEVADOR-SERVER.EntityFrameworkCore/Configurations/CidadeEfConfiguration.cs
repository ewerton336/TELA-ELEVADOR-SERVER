using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Configurations;

public sealed class CidadeEfConfiguration : IEntityTypeConfiguration<Cidade>
{
    public void Configure(EntityTypeBuilder<Cidade> builder)
    {
        builder.ToTable("Cidade");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).UseIdentityByDefaultColumn();

        // Índice único no nome normalizado
        builder.HasIndex(c => c.Nome)
            .IsUnique()
            .HasDatabaseName("IX_Cidade_Nome_Unique");

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.NomeExibicao)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Latitude)
            .IsRequired();

        builder.Property(c => c.Longitude)
            .IsRequired();

        // Relacionamento com Predio
        builder.HasMany(c => c.Predios)
            .WithOne(p => p.Cidade)
            .HasForeignKey(p => p.CidadeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relacionamento com ClimaPrevisao
        builder.HasMany(c => c.ClimaPrevisoesData)
            .WithOne(cp => cp.Cidade)
            .HasForeignKey(cp => cp.CidadeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
