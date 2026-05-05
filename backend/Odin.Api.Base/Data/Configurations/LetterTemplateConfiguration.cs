using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class LetterTemplateConfiguration : IEntityTypeConfiguration<LetterTemplate>
{
    public void Configure(EntityTypeBuilder<LetterTemplate> builder)
    {
        builder.HasKey(e => e.LetterTemplateId);

        builder.Property(e => e.LetterType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.BodyHtml).HasColumnType("text");
        builder.Property(e => e.CertificateBackgroundPath).HasMaxLength(512);
        builder.Property(e => e.CertificateLayoutJson).HasColumnType("jsonb");

        builder.Property(e => e.IsPublished)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(e => new { e.ProgrammeId, e.LetterType })
            .HasFilter("\"DeletedAt\" IS NULL")
            .IsUnique();

        builder.HasOne(e => e.Programme)
            .WithMany()
            .HasForeignKey(e => e.ProgrammeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
