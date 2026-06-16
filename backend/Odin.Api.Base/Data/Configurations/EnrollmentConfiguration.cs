using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.StudentEnrollmentId);

        // Stable letter reference code. Unique so a verify lookup resolves to
        // exactly one enrolment; Postgres treats nulls as distinct, so the
        // many enrolments that have not yet released a letter coexist fine.
        builder.Property(e => e.LetterReferenceCode).HasMaxLength(16);
        builder.HasIndex(e => e.LetterReferenceCode).IsUnique();

        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Partner)
            .WithMany()
            .HasForeignKey(e => e.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Specialization)
            .WithMany()
            .HasForeignKey(e => e.SpecializationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ModeOfStudy)
            .WithMany()
            .HasForeignKey(e => e.ModeOfStudyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Status)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // Domain quirk: Enrollment.PathwayId is `int` but Pathway.PathwayId is
        // `Guid`. EF cannot wire a real FK across mismatched key types, so the
        // navigation is ignored — callers must look up Pathway separately when
        // needed.
        builder.Ignore(e => e.Pathway);
    }
}
