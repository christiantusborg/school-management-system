using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Assignments;

namespace Odin.Api.Base.Data.Configurations;

public class AssignmentUploadConfiguration : IEntityTypeConfiguration<AssignmentUpload>
{
    public void Configure(EntityTypeBuilder<AssignmentUpload> builder)
    {
        builder.HasKey(e => e.AssignmentUploadId);
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.MimeType).HasMaxLength(150).IsRequired();
        builder.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(e => e.UploadedByRole).HasMaxLength(40).IsRequired();
        builder.Property(e => e.UploadedByName).HasMaxLength(200);
        builder.HasIndex(e => new { e.StudentEnrollmentId, e.SubjectId });

        builder.HasMany(e => e.Comments)
            .WithOne(c => c.Upload)
            .HasForeignKey(c => c.AssignmentUploadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AssignmentCommentConfiguration : IEntityTypeConfiguration<AssignmentComment>
{
    public void Configure(EntityTypeBuilder<AssignmentComment> builder)
    {
        builder.HasKey(e => e.AssignmentCommentId);
        builder.Property(e => e.AuthorRole).HasMaxLength(40).IsRequired();
        builder.Property(e => e.AuthorName).HasMaxLength(200);
        builder.Property(e => e.Text).HasMaxLength(4000).IsRequired();
    }
}
