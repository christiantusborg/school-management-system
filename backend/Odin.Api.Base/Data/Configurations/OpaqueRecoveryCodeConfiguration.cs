using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class OpaqueRecoveryCodeConfiguration : IEntityTypeConfiguration<OpaqueRecoveryCode>
{
    public void Configure(EntityTypeBuilder<OpaqueRecoveryCode> builder)
    {
        builder.HasKey(e => e.OpaqueRecoveryCodeId);
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
