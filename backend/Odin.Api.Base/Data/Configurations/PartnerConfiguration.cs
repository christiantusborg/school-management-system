using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.HasKey(e => e.PartnerId);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(200).IsRequired();
        builder.HasIndex(e => e.Slug)
            .HasFilter("\"DeletedAt\" IS NULL")
            .IsUnique();

        builder.HasMany(e => e.Addresses)
            .WithOne(a => a.Partner)
            .HasForeignKey(a => a.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Phones)
            .WithOne(p => p.Partner)
            .HasForeignKey(p => p.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Emails)
            .WithOne(p => p.Partner)
            .HasForeignKey(p => p.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Contracts)
            .WithOne()
            .HasForeignKey(c => c.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
