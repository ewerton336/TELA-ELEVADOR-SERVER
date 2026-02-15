using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.EntityFrameworkCore.Configurations;

public sealed class PredioEfConfiguration : IEntityTypeConfiguration<Predio>
{
    public void Configure(EntityTypeBuilder<Predio> builder)
    {
        builder.ToTable("Predio");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityByDefaultColumn();
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.OrientationMode)
            .HasMaxLength(20)
            .HasDefaultValue("auto");
    }
}
