using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Configurations;

public sealed class ClimaAtualEfConfiguration : IEntityTypeConfiguration<ClimaAtual>
{
    public void Configure(EntityTypeBuilder<ClimaAtual> builder)
    {
        builder.ToTable("ClimaAtual");
        builder.HasKey(ca => ca.Id);
        builder.Property(ca => ca.Id).UseIdentityByDefaultColumn();

        builder.HasIndex(ca => ca.CidadeId)
            .IsUnique()
            .HasDatabaseName("IX_ClimaAtual_CidadeId_Unique");

        builder.Property(ca => ca.CidadeId).IsRequired();
        builder.Property(ca => ca.Temperatura).IsRequired();
        builder.Property(ca => ca.SensacaoTermica).IsRequired();
        builder.Property(ca => ca.Umidade).IsRequired();
        builder.Property(ca => ca.VentoVelocidade).IsRequired();
        builder.Property(ca => ca.CodigoWmo).IsRequired();
        builder.Property(ca => ca.Descricao).IsRequired().HasMaxLength(100);
        builder.Property(ca => ca.Icone).IsRequired().HasMaxLength(10);
        builder.Property(ca => ca.IsDay).IsRequired();
        builder.Property(ca => ca.AtualizadoEm).IsRequired();

        builder.HasOne(ca => ca.Cidade)
            .WithOne(c => c.ClimaAtual)
            .HasForeignKey<ClimaAtual>(ca => ca.CidadeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
