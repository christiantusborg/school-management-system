using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class ProgrammePathwayConfiguration : IEntityTypeConfiguration<ProgrammePathway>
{
    public void Configure(EntityTypeBuilder<ProgrammePathway> builder)
    {
        builder.HasKey(e => e.ProgrammePathwayId);
        builder.HasOne(e => e.Programme)
            .WithMany()
            .HasForeignKey(e => e.ProgrammeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Pathway)
            .WithMany()
            .HasForeignKey(e => e.PathwayId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.ProgrammeId, e.PathwayId })
            .HasFilter("\"DeletedAt\" IS NULL")
            .IsUnique();
    }
}
