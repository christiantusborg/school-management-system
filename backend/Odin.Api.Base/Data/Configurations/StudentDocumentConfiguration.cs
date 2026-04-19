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
        builder.HasOne(e => e.VerifiedBy)
            .WithMany()
            .HasForeignKey(e => e.VerifiedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
