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
        builder.Property(e => e.IsLegacyStudent).IsRequired().HasDefaultValue(false);
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Nationality)
            .WithMany()
            .HasForeignKey(e => e.NationalityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional background lookups — Restrict so a lookup row that's in use
        // can't be hard-deleted out from under a student (the lists soft-delete).
        builder.HasOne(e => e.CurrentPositionFunction)
            .WithMany()
            .HasForeignKey(e => e.CurrentPositionFunctionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.CurrentEmploymentIndustry)
            .WithMany()
            .HasForeignKey(e => e.CurrentEmploymentIndustryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.MonthlySalaryCurrency)
            .WithMany()
            .HasForeignKey(e => e.MonthlySalaryCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.MonthlySalaryAmount).HasPrecision(18, 2);

        // Domain quirk: Student.Languages is `ICollection<UserLanguage>`, but
        // UserLanguage.UserId is `Guid` while Student.UserId is `string` — EF
        // cannot wire the FK. Ignore the navigation; queries must hit the
        // UserLanguages DbSet directly.
        builder.Ignore(e => e.Languages);
    }
}
