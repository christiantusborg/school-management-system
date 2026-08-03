using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerSpecializationStatusConfiguration : IEntityTypeConfiguration<PartnerSpecializationStatus>
{
    public void Configure(EntityTypeBuilder<PartnerSpecializationStatus> builder)
    {
        builder.HasKey(e => e.SpecializationId);
        builder.Property(e => e.RejectionReason).HasMaxLength(2000);
        builder.HasOne(e => e.Specialization)
            .WithOne()
            .HasForeignKey<PartnerSpecializationStatus>(e => e.SpecializationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
