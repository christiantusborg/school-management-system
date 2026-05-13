using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class LetterAssetConfiguration : IEntityTypeConfiguration<LetterAsset>
{
    public void Configure(EntityTypeBuilder<LetterAsset> builder)
    {
        builder.HasKey(e => e.LetterAssetId);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.MimeType).HasMaxLength(80).IsRequired();
        builder.Property(e => e.StoragePath).HasMaxLength(512).IsRequired();
        builder.HasIndex(e => e.Name).HasFilter("\"DeletedAt\" IS NULL").IsUnique();
    }
}
