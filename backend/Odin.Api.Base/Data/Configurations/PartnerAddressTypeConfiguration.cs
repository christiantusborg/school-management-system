using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerAddressTypeConfiguration : IEntityTypeConfiguration<PartnerAddressType>
{
    public void Configure(EntityTypeBuilder<PartnerAddressType> builder)
    {
        builder.HasKey(e => e.PartnerAddressTypeId);
        builder.Property(e => e.Code).HasMaxLength(40).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();
    }
}
