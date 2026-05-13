using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class ProgrammePartnerConfiguration : IEntityTypeConfiguration<ProgrammePartner>
{
    public void Configure(EntityTypeBuilder<ProgrammePartner> builder)
    {
        builder.HasKey(e => e.ProgrammePartnerId);
        builder.HasOne(e => e.Programme)
            .WithMany()
            .HasForeignKey(e => e.ProgrammeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Partner)
            .WithMany()
            .HasForeignKey(e => e.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.ProgrammeId, e.PartnerId }).IsUnique();
    }
}
