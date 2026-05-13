using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerUsersConfiguration : IEntityTypeConfiguration<PartnerUsers>
{
    public void Configure(EntityTypeBuilder<PartnerUsers> builder)
    {
        builder.HasKey(e => new { e.PartnerId, e.UserId });
        builder.HasOne<Partner>()
            .WithMany(p => p.Users)
            .HasForeignKey(e => e.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
