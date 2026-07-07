using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class SpecializationConfiguration : IEntityTypeConfiguration<Specialization>
{
    public void Configure(EntityTypeBuilder<Specialization> builder)
    {
        builder.HasKey(e => e.SpecializationId);
        builder.Property(e => e.Code).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(e => new { e.ProgrammeId, e.Code })
            .HasFilter("\"DeletedAt\" IS NULL")
            .IsUnique();
        builder.Property(e => e.TuitionFeeUsd)
            .HasPrecision(10, 2)
            .HasDefaultValue(0m);
        builder.Property(e => e.OfferAcceptanceMode)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(OfferAcceptanceMode.StudentAccept);
        // IBSS is English-medium: default the study language to English at the
        // DB level so a specialization is never languageless on any insert path.
        builder.Property(e => e.InstructionLanguage).HasMaxLength(120).HasDefaultValue("English");
    }
}
