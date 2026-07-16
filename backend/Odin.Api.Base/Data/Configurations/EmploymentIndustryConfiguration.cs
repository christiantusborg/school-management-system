using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class EmploymentIndustryConfiguration : IEntityTypeConfiguration<EmploymentIndustry>
{
    public void Configure(EntityTypeBuilder<EmploymentIndustry> builder)
    {
        builder.HasKey(e => e.EmploymentIndustryId);
        builder.Property(e => e.Name).HasMaxLength(120).IsRequired();
    }
}
