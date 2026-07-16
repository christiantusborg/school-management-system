using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace Odin.Api.Base.Data.Configurations;

public class FieldLibraryEntryConfiguration : IEntityTypeConfiguration<FieldLibraryEntry>
{
    public void Configure(EntityTypeBuilder<FieldLibraryEntry> builder)
    {
        builder.HasKey(e => e.FieldLibraryEntryId);
        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(64).IsRequired();
        builder.Property(e => e.DefinitionJson).HasColumnType("text").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasMaxLength(450);
    }
}

public class TextTemplateConfiguration : IEntityTypeConfiguration<TextTemplate>
{
    public void Configure(EntityTypeBuilder<TextTemplate> builder)
    {
        builder.HasKey(e => e.TextTemplateId);
        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.Property(e => e.BodyJson).HasColumnType("text").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasMaxLength(450);
    }
}

public class GenerationRuleConfiguration : IEntityTypeConfiguration<GenerationRule>
{
    public void Configure(EntityTypeBuilder<GenerationRule> builder)
    {
        builder.HasKey(e => e.GenerationRuleId);
        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.Property(e => e.RuleJson).HasColumnType("text").IsRequired();
        builder.Property(e => e.IncludeDocumentTemplateIdsCsv).HasColumnType("text").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasMaxLength(450);
    }
}

public class DocumentTemplateConfiguration : IEntityTypeConfiguration<DocumentTemplate>
{
    public void Configure(EntityTypeBuilder<DocumentTemplate> builder)
    {
        builder.HasKey(e => e.DocumentTemplateId);
        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.Property(e => e.BaseAssetRef).HasMaxLength(512);
        builder.Property(e => e.MappingJson).HasColumnType("text").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasMaxLength(450);
    }
}

public class DocumentTemplateAssetConfiguration : IEntityTypeConfiguration<DocumentTemplateAsset>
{
    public void Configure(EntityTypeBuilder<DocumentTemplateAsset> builder)
    {
        builder.HasKey(e => e.DocumentTemplateAssetId);
        builder.HasIndex(e => e.DocumentTemplateId).IsUnique();
        builder.Property(e => e.Filename).HasMaxLength(512).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(128).IsRequired();

        builder.HasOne(e => e.DocumentTemplate)
            .WithOne()
            .HasForeignKey<DocumentTemplateAsset>(e => e.DocumentTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DocumentTemplateImageConfiguration : IEntityTypeConfiguration<DocumentTemplateImage>
{
    public void Configure(EntityTypeBuilder<DocumentTemplateImage> builder)
    {
        builder.HasKey(e => e.DocumentTemplateImageId);
        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.Property(e => e.MimeType).HasMaxLength(128).IsRequired();
        builder.Property(e => e.DataBase64).HasColumnType("text").IsRequired();
        builder.Property(e => e.UploadedByUserId).HasMaxLength(450);
    }
}

public class IntakeOutputConfiguration : IEntityTypeConfiguration<IntakeOutput>
{
    public void Configure(EntityTypeBuilder<IntakeOutput> builder)
    {
        builder.HasKey(e => e.IntakeOutputId);
        builder.HasIndex(e => e.IntakeResponseId);
        builder.Property(e => e.FileName).HasMaxLength(512).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(e => e.StoragePath).HasMaxLength(1024).IsRequired();

        builder.HasOne(e => e.IntakeResponse)
            .WithMany()
            .HasForeignKey(e => e.IntakeResponseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.DocumentTemplate)
            .WithMany()
            .HasForeignKey(e => e.DocumentTemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
