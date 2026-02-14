using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Configurations;

public sealed class FonteNoticiaEfConfiguration : IEntityTypeConfiguration<FonteNoticia>
{
    public void Configure(EntityTypeBuilder<FonteNoticia> builder)
    {
        builder.ToTable("FonteNoticia");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).UseIdentityByDefaultColumn();
        builder.HasIndex(f => f.Chave).IsUnique();
    }
}
