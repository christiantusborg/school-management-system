using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class StudentNoteConfiguration : IEntityTypeConfiguration<StudentNote>
{
    public void Configure(EntityTypeBuilder<StudentNote> builder)
    {
        builder.HasKey(e => e.StudentNoteId);
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Notes)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.StudentEnrollment)
            .WithMany()
            .HasForeignKey(e => e.StudentEnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
