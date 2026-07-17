using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class EnrollmentModuleStartConfiguration : IEntityTypeConfiguration<EnrollmentModuleStart>
{
    public void Configure(EntityTypeBuilder<EnrollmentModuleStart> builder)
    {
        builder.HasKey(e => e.EnrollmentModuleStartId);
        builder.HasIndex(e => new { e.StudentEnrollmentId, e.SubjectId }).IsUnique();
    }
}
