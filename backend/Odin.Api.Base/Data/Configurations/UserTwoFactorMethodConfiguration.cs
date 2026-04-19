using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class UserTwoFactorMethodConfiguration : IEntityTypeConfiguration<UserTwoFactorMethod>
{
    public void Configure(EntityTypeBuilder<UserTwoFactorMethod> builder)
    {
        builder.HasKey(e => e.UserTwoFactorMethodId);
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
