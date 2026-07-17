using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class PartnerDatasheetDefinitionConfiguration : IEntityTypeConfiguration<PartnerDatasheetDefinition>
{
    public void Configure(EntityTypeBuilder<PartnerDatasheetDefinition> builder)
    {
        builder.HasKey(e => e.PartnerDatasheetDefinitionId);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
    }
}

public class PartnerDatasheetSectionConfiguration : IEntityTypeConfiguration<PartnerDatasheetSection>
{
    public void Configure(EntityTypeBuilder<PartnerDatasheetSection> builder)
    {
        builder.HasKey(e => e.PartnerDatasheetSectionId);
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Kind).HasMaxLength(20).IsRequired();
        builder.HasIndex(e => e.PartnerDatasheetDefinitionId);
    }
}

public class PartnerDatasheetFieldConfiguration : IEntityTypeConfiguration<PartnerDatasheetField>
{
    public void Configure(EntityTypeBuilder<PartnerDatasheetField> builder)
    {
        builder.HasKey(e => e.PartnerDatasheetFieldId);
        builder.Property(e => e.Label).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(20).IsRequired();
        builder.HasIndex(e => e.PartnerDatasheetSectionId);
    }
}

public class PartnerDatasheetConfiguration : IEntityTypeConfiguration<PartnerDatasheet>
{
    public void Configure(EntityTypeBuilder<PartnerDatasheet> builder)
    {
        builder.HasKey(e => e.PartnerDatasheetId);
        builder.Property(e => e.Title).HasMaxLength(300);
        builder.HasIndex(e => e.PartnerId);
        builder.HasIndex(e => e.PartnerDatasheetDefinitionId);
    }
}

public class PartnerDatasheetRowConfiguration : IEntityTypeConfiguration<PartnerDatasheetRow>
{
    public void Configure(EntityTypeBuilder<PartnerDatasheetRow> builder)
    {
        builder.HasKey(e => e.PartnerDatasheetRowId);
        builder.HasIndex(e => e.PartnerDatasheetId);
    }
}

public class PartnerDatasheetValueConfiguration : IEntityTypeConfiguration<PartnerDatasheetValue>
{
    public void Configure(EntityTypeBuilder<PartnerDatasheetValue> builder)
    {
        builder.HasKey(e => e.PartnerDatasheetValueId);
        builder.Property(e => e.FileName).HasMaxLength(300);
        // One cell per (row, field) — upserts are deterministic.
        builder.HasIndex(e => new { e.PartnerDatasheetRowId, e.PartnerDatasheetFieldId }).IsUnique();
    }
}
