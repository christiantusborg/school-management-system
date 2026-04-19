using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class KemKeyPairConfiguration : IEntityTypeConfiguration<KemKeyPair>
{
    public void Configure(EntityTypeBuilder<KemKeyPair> builder)
    {
        builder.HasKey(e => e.UserId);
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
