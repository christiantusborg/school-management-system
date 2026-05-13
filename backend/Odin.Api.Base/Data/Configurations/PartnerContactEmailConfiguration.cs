using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerContactEmailConfiguration : IEntityTypeConfiguration<PartnerContactEmail>
{
    public void Configure(EntityTypeBuilder<PartnerContactEmail> builder)
    {
        builder.HasKey(e => e.PartnerContactEmailId);
        builder.Property(e => e.Email).HasMaxLength(320);
    }
}
