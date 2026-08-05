using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class StudentIdentifierConfiguration : IEntityTypeConfiguration<StudentIdentifier>
{
    public void Configure(EntityTypeBuilder<StudentIdentifier> builder)
    {
        builder.HasKey(e => e.StudentIdentifierId);
        builder.Property(e => e.Value).HasMaxLength(100);
        builder.Property(e => e.Label).HasMaxLength(200);
        builder.HasIndex(e => e.Value).IsUnique();
        builder.HasIndex(e => e.StudentId);
        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
