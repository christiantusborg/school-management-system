using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace Odin.Api.Base.Data.Configurations;

public class BulkAgreementConfiguration : IEntityTypeConfiguration<BulkAgreement>
{
    public void Configure(EntityTypeBuilder<BulkAgreement> builder)
    {
        builder.HasKey(e => e.BulkAgreementId);
        builder.Property(e => e.AgreementNumber).HasMaxLength(200);
        builder.Property(e => e.Note).HasMaxLength(2000);
        builder.HasOne(e => e.Partner)
            .WithMany()
            .HasForeignKey(e => e.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.PartnerId);
    }
}

public class BulkAgreementSpecializationConfiguration : IEntityTypeConfiguration<BulkAgreementSpecialization>
{
    public void Configure(EntityTypeBuilder<BulkAgreementSpecialization> builder)
    {
        builder.HasKey(e => e.BulkAgreementSpecializationId);
        builder.HasIndex(e => new { e.BulkAgreementId, e.SpecializationId }).IsUnique();
        builder.HasOne(e => e.Agreement)
            .WithMany(a => a.Specializations)
            .HasForeignKey(e => e.BulkAgreementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
