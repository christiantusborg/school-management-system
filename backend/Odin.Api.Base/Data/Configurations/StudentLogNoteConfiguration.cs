using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class StudentLogNoteConfiguration : IEntityTypeConfiguration<StudentLogNote>
{
    public void Configure(EntityTypeBuilder<StudentLogNote> builder)
    {
        builder.HasKey(e => e.StudentLogNoteId);
        builder.Property(e => e.Title).HasMaxLength(300);
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.AuthorRole).HasMaxLength(20).IsRequired();
        builder.Property(e => e.AuthorName).HasMaxLength(200);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.StudentEnrollmentId);
    }
}
