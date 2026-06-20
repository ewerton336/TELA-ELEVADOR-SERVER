using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Configurations;

public sealed class TickerMensagemEfConfiguration : IEntityTypeConfiguration<TickerMensagem>
{
    public void Configure(EntityTypeBuilder<TickerMensagem> builder)
    {
        builder.ToTable("TickerMensagem");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseIdentityByDefaultColumn();
        builder.Property(t => t.Texto).HasMaxLength(500).IsRequired();
        builder.Property(t => t.Ativo).HasDefaultValue(true);
        builder.Property(t => t.Ordem).HasDefaultValue(0);
        builder.HasIndex(t => t.PredioId);
        builder.HasOne(t => t.Predio)
            .WithMany(p => p.TickerMensagens)
            .HasForeignKey(t => t.PredioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
