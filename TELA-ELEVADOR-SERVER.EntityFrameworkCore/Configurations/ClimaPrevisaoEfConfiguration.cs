using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Configurations;

public sealed class ClimaPrevisaoEfConfiguration : IEntityTypeConfiguration<ClimaPrevisao>
{
    public void Configure(EntityTypeBuilder<ClimaPrevisao> builder)
    {
        builder.ToTable("ClimaPrevisao");
        builder.HasKey(cp => cp.Id);
        builder.Property(cp => cp.Id).UseIdentityByDefaultColumn();

        // Índice composto único para evitar duplicatas
        builder.HasIndex(cp => new { cp.CidadeId, cp.Data })
            .IsUnique()
            .HasDatabaseName("IX_ClimaPrevisao_CidadeId_Data_Unique");

        builder.Property(cp => cp.CidadeId)
            .IsRequired();

        builder.Property(cp => cp.Data)
            .IsRequired();

        builder.Property(cp => cp.TemperaturaMaxima)
            .IsRequired();

        builder.Property(cp => cp.TemperaturaMinima)
            .IsRequired();

        builder.Property(cp => cp.CodigoWmo)
            .IsRequired();

        builder.Property(cp => cp.Descricao)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cp => cp.Icone)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(cp => cp.AtualizadoEm)
            .IsRequired();

        // Foreign Key
        builder.HasOne(cp => cp.Cidade)
            .WithMany(c => c.ClimaPrevisoesData)
            .HasForeignKey(cp => cp.CidadeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
