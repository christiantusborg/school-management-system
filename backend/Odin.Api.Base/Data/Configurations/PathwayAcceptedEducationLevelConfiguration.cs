using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class PathwayAcceptedEducationLevelConfiguration : IEntityTypeConfiguration<PathwayAcceptedEducationLevel>
{
    public void Configure(EntityTypeBuilder<PathwayAcceptedEducationLevel> builder)
    {
        builder.HasKey(e => new { e.PathwayId, e.EducationLevelId });
        builder.HasOne(e => e.Pathway)
            .WithMany(p => p.AcceptedEducationLevels)
            .HasForeignKey(e => e.PathwayId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.EducationLevel)
            .WithMany()
            .HasForeignKey(e => e.EducationLevelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
