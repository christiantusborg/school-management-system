using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class StudentDocumentConfiguration : IEntityTypeConfiguration<StudentDocument>
{
    public void Configure(EntityTypeBuilder<StudentDocument> builder)
    {
        builder.HasKey(e => e.StudentDocumentId);
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Documents)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.DocumentType)
            .WithMany()
            .HasForeignKey(e => e.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.CurrentStatus)
            .WithMany()
            .HasForeignKey(e => e.CurrentStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Enrollment)
            .WithMany()
            .HasForeignKey(e => e.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // One active row per (Enrollment, DocumentType). Filtered to
        // non-deleted rows that have completed wizard attribution
        // (EnrollmentId IS NOT NULL) so wizard-pending uploads don't trip it.
        builder.HasIndex(e => new { e.EnrollmentId, e.DocumentTypeId })
            .IsUnique()
            .HasFilter("\"EnrollmentId\" IS NOT NULL AND \"DeletedAt\" IS NULL");
    }
}
