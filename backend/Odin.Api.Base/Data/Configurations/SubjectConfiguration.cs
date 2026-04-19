using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.HasKey(e => e.SubjectId);
        builder.HasOne(e => e.Major)
            .WithMany(m => m.Subjects)
            .HasForeignKey(e => e.MajorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
