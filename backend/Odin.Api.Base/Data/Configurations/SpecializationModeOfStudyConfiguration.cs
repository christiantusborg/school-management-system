using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class SpecializationModeOfStudyConfiguration : IEntityTypeConfiguration<SpecializationModeOfStudy>
{
    public void Configure(EntityTypeBuilder<SpecializationModeOfStudy> builder)
    {
        builder.HasKey(e => e.SpecializationModeOfStudyId);
        builder.HasIndex(e => new { e.SpecializationId, e.ModeOfStudyId }).IsUnique();
        builder.HasOne<Specialization>()
            .WithMany()
            .HasForeignKey(e => e.SpecializationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Domain quirk: SpecializationModeOfStudy.ModeOfStudyId is `Guid` but
        // ModeOfStudy.ModeOfStudyId is `int`. EF cannot wire a real FK across
        // mismatched key types — leave this column as a plain Guid with no
        // navigation. Callers must look up ModeOfStudy separately when needed.
        builder.Ignore(e => e.ModesOfStudy);
        builder.Ignore(e => e.Subjects);
    }
}
