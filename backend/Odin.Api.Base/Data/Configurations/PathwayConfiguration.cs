using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class PathwayConfiguration : IEntityTypeConfiguration<Pathway>
{
    public void Configure(EntityTypeBuilder<Pathway> builder)
    {
        builder.HasKey(e => e.PathwayId);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000).IsRequired();
    }
}
