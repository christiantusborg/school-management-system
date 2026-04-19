using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class FinalProjectConfiguration : IEntityTypeConfiguration<FinalProject>
{
    public void Configure(EntityTypeBuilder<FinalProject> builder)
    {
        builder.HasKey(e => e.FinalProjectId);
        builder.HasIndex(e => e.StudentEnrollmentId).IsUnique();
        builder.HasOne(e => e.StudentEnrollment)
            .WithOne(se => se.FinalProject)
            .HasForeignKey<FinalProject>(e => e.StudentEnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.FinalProjectStatus)
            .WithMany()
            .HasForeignKey(e => e.FinalProjectStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
