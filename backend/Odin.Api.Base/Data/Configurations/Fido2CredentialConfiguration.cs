using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class Fido2CredentialConfiguration : IEntityTypeConfiguration<Fido2Credential>
{
    public void Configure(EntityTypeBuilder<Fido2Credential> builder)
    {
        builder.HasKey(e => e.Fido2CredentialId);
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
