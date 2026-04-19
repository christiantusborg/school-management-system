using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class SessionTokenConfiguration : IEntityTypeConfiguration<SessionToken>
{
    public void Configure(EntityTypeBuilder<SessionToken> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TokenHash).IsUnique();
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
