using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class ProgrammeDocumentRequirementConfiguration : IEntityTypeConfiguration<ProgrammeDocumentRequirement>
{
    public void Configure(EntityTypeBuilder<ProgrammeDocumentRequirement> builder)
    {
        builder.HasKey(e => e.ProgrammeDocumentRequirementId);
        builder.HasOne(e => e.Programme)
            .WithMany(p => p.RequiredDocumentTypes)
            .HasForeignKey(e => e.ProgrammeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.DocumentType)
            .WithMany()
            .HasForeignKey(e => e.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.ProgrammeId, e.DocumentTypeId })
            .HasFilter("\"DeletedAt\" IS NULL")
            .IsUnique();
    }
}
