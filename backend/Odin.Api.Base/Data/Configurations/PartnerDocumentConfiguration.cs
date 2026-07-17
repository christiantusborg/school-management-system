using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerDocumentTypeConfiguration : IEntityTypeConfiguration<PartnerDocumentType>
{
    public void Configure(EntityTypeBuilder<PartnerDocumentType> builder)
    {
        builder.HasKey(e => e.PartnerDocumentTypeId);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
    }
}

public class PartnerDocumentConfiguration : IEntityTypeConfiguration<PartnerDocument>
{
    public void Configure(EntityTypeBuilder<PartnerDocument> builder)
    {
        builder.HasKey(e => e.PartnerDocumentId);
        builder.HasIndex(e => e.PartnerId);
        builder.HasIndex(e => e.PartnerDocumentTypeId);
    }
}
