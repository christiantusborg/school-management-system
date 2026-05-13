using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class SubjectGradeConfiguration : IEntityTypeConfiguration<SubjectGrade>
{
    public void Configure(EntityTypeBuilder<SubjectGrade> builder)
    {
        builder.HasKey(e => e.SubjectGradeId);
        builder.HasOne(e => e.Enrollment)
            .WithMany(en => en.SubjectGrades)
            .HasForeignKey(e => e.StudentEnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Subject)
            .WithMany()
            .HasForeignKey(e => e.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
