using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerContractConfiguration : IEntityTypeConfiguration<PartnerContract>
{
    public void Configure(EntityTypeBuilder<PartnerContract> builder)
    {
        builder.HasKey(e => e.PartnerContractId);
        builder.HasOne(e => e.Partner)
            .WithMany(p => p.Contracts)
            .HasForeignKey(e => e.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Notes)
            .WithOne(n => n.Contract)
            .HasForeignKey(n => n.PartnerContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
