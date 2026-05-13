using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class EnrollmentStatusNoteConfiguration : IEntityTypeConfiguration<EnrollmentStatusNote>
{
    public void Configure(EntityTypeBuilder<EnrollmentStatusNote> builder)
    {
        builder.HasKey(e => e.EnrollmentStatusNoteId);
        builder.Property(e => e.Note).IsRequired();
        builder.HasIndex(e => e.EnrollmentId);
        builder.HasOne(e => e.Status)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
