using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(e => e.StudentId);
        builder.HasIndex(e => e.UserId).IsUnique();
        builder.HasIndex(e => e.StudentNumber).IsUnique();
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Nationality)
            .WithMany()
            .HasForeignKey(e => e.NationalityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Domain quirk: Student.Languages is `ICollection<UserLanguage>`, but
        // UserLanguage.UserId is `Guid` while Student.UserId is `string` — EF
        // cannot wire the FK. Ignore the navigation; queries must hit the
        // UserLanguages DbSet directly.
        builder.Ignore(e => e.Languages);
    }
}
