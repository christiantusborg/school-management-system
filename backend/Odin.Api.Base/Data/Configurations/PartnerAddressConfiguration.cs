using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerAddressConfiguration : IEntityTypeConfiguration<PartnerAddress>
{
    public void Configure(EntityTypeBuilder<PartnerAddress> builder)
    {
        builder.HasKey(e => e.PartnerAddressId);
        // Widened from 2: the partner Profile form stores a free-text
        // country name here, not an ISO code.
        builder.Property(e => e.CountryCode).HasMaxLength(64).IsRequired();
        builder.HasOne(e => e.Type)
            .WithMany()
            .HasForeignKey(e => e.PartnerAddressTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
