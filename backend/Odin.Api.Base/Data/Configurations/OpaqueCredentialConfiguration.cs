using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class OpaqueCredentialConfiguration : IEntityTypeConfiguration<OpaqueCredential>
{
    public void Configure(EntityTypeBuilder<OpaqueCredential> builder)
    {
        builder.HasKey(e => e.OpaqueCredentialId);
        builder.HasIndex(e => e.UserId).IsUnique();
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
