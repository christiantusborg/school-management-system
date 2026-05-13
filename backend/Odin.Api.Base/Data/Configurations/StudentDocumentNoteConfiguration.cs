using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class StudentDocumentNoteConfiguration : IEntityTypeConfiguration<StudentDocumentNote>
{
    public void Configure(EntityTypeBuilder<StudentDocumentNote> builder)
    {
        builder.HasKey(e => e.StudentDocumentNoteId);
        builder.HasIndex(e => e.StudentDocumentId);
        builder.HasOne(e => e.StudentDocument)
            .WithMany()
            .HasForeignKey(e => e.StudentDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Status)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
