using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class SpecializationPartnerConfiguration : IEntityTypeConfiguration<SpecializationPartner>
{
    public void Configure(EntityTypeBuilder<SpecializationPartner> builder)
    {
        builder.HasKey(e => e.SpecializationPartnerId);
        builder.HasIndex(e => new { e.SpecializationId, e.PartnerId }).IsUnique();
        builder.HasOne(e => e.Specialization)
            .WithMany()
            .HasForeignKey(e => e.SpecializationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Partner)
            .WithMany()
            .HasForeignKey(e => e.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
