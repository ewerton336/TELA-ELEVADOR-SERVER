using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Configurations;

public sealed class NoticiaInternaEfConfiguration : IEntityTypeConfiguration<NoticiaInterna>
{
    public void Configure(EntityTypeBuilder<NoticiaInterna> builder)
    {
        builder.ToTable("NoticiaInterna");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).UseIdentityByDefaultColumn();
        builder.Property(n => n.Titulo).HasMaxLength(300);
        builder.Property(n => n.Subtitulo).HasMaxLength(500);
        builder.Property(n => n.TipoMidia).HasMaxLength(20).IsRequired();
        builder.Property(n => n.NomeArquivo).HasMaxLength(260).IsRequired();
        builder.Property(n => n.NomeArquivoOriginal).HasMaxLength(260).IsRequired();
        builder.Property(n => n.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(n => n.Ativo).HasDefaultValue(true);

        builder.HasIndex(n => n.PredioId);

        builder.HasOne(n => n.Predio)
            .WithMany(p => p.NoticiasInternas)
            .HasForeignKey(n => n.PredioId);
    }
}
