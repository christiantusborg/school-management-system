using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class UserLanguageConfiguration : IEntityTypeConfiguration<UserLanguage>
{
    public void Configure(EntityTypeBuilder<UserLanguage> builder)
    {
        // PK is currently mis-named `StudentLanguageId` (rename leftover) — kept
        // as-is so EF maps to the existing column. When the domain renames it
        // to `UserLanguageId`, update this property reference.
        builder.HasKey(e => e.StudentLanguageId);

        builder.HasOne(e => e.Language)
            .WithMany()
            .HasForeignKey(e => e.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        // Domain quirk: UserLanguage.UserId is `Guid` while ApplicationUser.Id is
        // `string` (Identity). The two cannot FK directly. Pending domain fix:
        // when UserId is retyped to `string`, replace the line below with
        //   builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        // and add a unique index on (UserId, LanguageId).

        // Skip the Student.Languages collection nav: Student.UserId is `string`
        // and won't FK to UserLanguage.UserId(Guid). EF Ignore on the Student
        // side keeps the model valid.
    }
}
