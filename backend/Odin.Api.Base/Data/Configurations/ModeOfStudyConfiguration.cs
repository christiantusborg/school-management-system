using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class ModeOfStudyConfiguration : IEntityTypeConfiguration<ModeOfStudy>
{
    public void Configure(EntityTypeBuilder<ModeOfStudy> builder)
    {
        builder.HasKey(e => e.ModeOfStudyId);
        builder.Property(e => e.ModeOfStudyId).ValueGeneratedNever();
        builder.HasData(
            new ModeOfStudy { ModeOfStudyId = 1, Name = "Distance/Online Self-Study" },
            new ModeOfStudy { ModeOfStudyId = 2, Name = "Blended Learning" },
            new ModeOfStudy { ModeOfStudyId = 3, Name = "Full-Time On-Campus" });
    }
}
