using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerProgrammeStatusConfiguration : IEntityTypeConfiguration<PartnerProgrammeStatus>
{
    public void Configure(EntityTypeBuilder<PartnerProgrammeStatus> builder)
    {
        builder.HasKey(e => e.ProgrammeId);
        builder.Property(e => e.RejectionReason).HasMaxLength(2000);
        builder.HasOne(e => e.Programme)
            .WithOne()
            .HasForeignKey<PartnerProgrammeStatus>(e => e.ProgrammeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
