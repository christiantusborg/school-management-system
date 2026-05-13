using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class EnrollmentStatusConfiguration : IEntityTypeConfiguration<EnrollmentStatus>
{
    public void Configure(EntityTypeBuilder<EnrollmentStatus> builder)
    {
        builder.HasKey(e => e.EnrollmentStatusId);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Code)
            .HasFilter("\"DeletedAt\" IS NULL")
            .IsUnique();
        builder.Property(e => e.NextActionRole).HasMaxLength(20);
        builder.HasOne(e => e.NextStatusOnComplete)
            .WithMany()
            .HasForeignKey(e => e.NextStatusOnCompleteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
