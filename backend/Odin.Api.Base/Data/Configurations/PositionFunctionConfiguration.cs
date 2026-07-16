using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class PositionFunctionConfiguration : IEntityTypeConfiguration<PositionFunction>
{
    public void Configure(EntityTypeBuilder<PositionFunction> builder)
    {
        builder.HasKey(e => e.PositionFunctionId);
        builder.Property(e => e.Name).HasMaxLength(120).IsRequired();
    }
}
