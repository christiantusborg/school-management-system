using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerProgrammeAccessConfiguration : IEntityTypeConfiguration<PartnerProgrammeAccess>
{
    public void Configure(EntityTypeBuilder<PartnerProgrammeAccess> builder)
    {
        builder.HasKey(e => e.PartnerProgrammeAccessId);

        builder.HasIndex(e => new { e.PartnerId, e.MajorId, e.DeletedAt });
        builder.HasIndex(e => e.PartnerId);

        builder.HasOne(e => e.Partner)
            .WithMany()
            .HasForeignKey(e => e.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Programme)
            .WithMany()
            .HasForeignKey(e => e.ProgrammeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Major)
            .WithMany()
            .HasForeignKey(e => e.MajorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
