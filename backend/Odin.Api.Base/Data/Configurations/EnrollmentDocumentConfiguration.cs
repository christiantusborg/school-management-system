using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class EnrollmentDocumentConfiguration : IEntityTypeConfiguration<EnrollmentDocument>
{
    public void Configure(EntityTypeBuilder<EnrollmentDocument> builder)
    {
        builder.HasKey(e => e.EnrollmentDocumentId);
        builder.HasOne(e => e.StudentEnrollment)
            .WithMany(se => se.EnrollmentDocuments)
            .HasForeignKey(e => e.StudentEnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.StudentDocument)
            .WithMany()
            .HasForeignKey(e => e.StudentDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.DocumentType)
            .WithMany()
            .HasForeignKey(e => e.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ApprovedBy)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
