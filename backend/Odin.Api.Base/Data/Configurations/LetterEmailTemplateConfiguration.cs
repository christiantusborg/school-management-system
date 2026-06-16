using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class LetterEmailTemplateConfiguration : IEntityTypeConfiguration<LetterEmailTemplate>
{
    public void Configure(EntityTypeBuilder<LetterEmailTemplate> builder)
    {
        builder.HasKey(e => e.LetterEmailTemplateId);

        builder.Property(e => e.LetterType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.IsEmailEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.Subject).HasMaxLength(512);
        builder.Property(e => e.BodyHtml).HasColumnType("text");
        builder.Property(e => e.CcRecipientsJson).HasColumnType("jsonb");
        builder.Property(e => e.BccRecipientsJson).HasColumnType("jsonb");

        builder.HasIndex(e => new { e.ProgrammeId, e.LetterType })
            .HasFilter("\"DeletedAt\" IS NULL")
            .IsUnique();

        builder.HasOne(e => e.Programme)
            .WithMany()
            .HasForeignKey(e => e.ProgrammeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
