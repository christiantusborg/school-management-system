using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerCertificateConfiguration : IEntityTypeConfiguration<PartnerCertificate>
{
    public void Configure(EntityTypeBuilder<PartnerCertificate> builder)
    {
        builder.HasKey(e => e.PartnerCertificateId);
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        // One active certificate per (partner, school).
        builder.HasIndex(e => new { e.PartnerId, e.SchoolId })
            .HasFilter("\"DeletedAt\" IS NULL")
            .IsUnique();
    }
}
