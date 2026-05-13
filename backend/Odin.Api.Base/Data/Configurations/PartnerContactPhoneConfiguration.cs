using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerContactPhoneConfiguration : IEntityTypeConfiguration<PartnerContactPhone>
{
    public void Configure(EntityTypeBuilder<PartnerContactPhone> builder)
    {
        builder.HasKey(e => e.PartnerContactPhoneId);
        builder.Property(e => e.Phone).HasMaxLength(40);
    }
}
