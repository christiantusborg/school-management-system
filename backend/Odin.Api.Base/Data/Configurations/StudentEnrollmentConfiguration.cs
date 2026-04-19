using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class StudentEnrollmentConfiguration : IEntityTypeConfiguration<StudentEnrollment>
{
    public void Configure(EntityTypeBuilder<StudentEnrollment> builder)
    {
        builder.HasKey(e => e.StudentEnrollmentId);
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Programme)
            .WithMany()
            .HasForeignKey(e => e.ProgrammeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Major)
            .WithMany()
            .HasForeignKey(e => e.MajorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ModeOfStudy)
            .WithMany()
            .HasForeignKey(e => e.ModeOfStudyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Pathway)
            .WithMany()
            .HasForeignKey(e => e.PathwayId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.EnrollmentStatus)
            .WithMany()
            .HasForeignKey(e => e.EnrollmentStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.TuitionFeeStatus)
            .WithMany()
            .HasForeignKey(e => e.TuitionFeeStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
