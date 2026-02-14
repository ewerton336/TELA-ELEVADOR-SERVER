using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Configurations;

public sealed class NoticiaEfConfiguration : IEntityTypeConfiguration<Noticia>
{
    public void Configure(EntityTypeBuilder<Noticia> builder)
    {
        builder.ToTable("Noticia");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).UseIdentityByDefaultColumn();

        builder.Property(n => n.FonteChave).HasMaxLength(50).IsRequired();
        builder.Property(n => n.FonteNome).HasMaxLength(100).IsRequired();
        builder.Property(n => n.Titulo).HasMaxLength(300).IsRequired();
        builder.Property(n => n.Descricao).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.Link).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.ImagemUrl).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.PubDateRaw).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Categoria).HasMaxLength(120);

        builder.HasIndex(n => n.Link).IsUnique();
        builder.HasIndex(n => n.PublicadoEmUtc);
        builder.HasIndex(n => n.FonteChave);
    }
}
