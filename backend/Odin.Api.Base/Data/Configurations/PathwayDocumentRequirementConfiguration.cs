using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class PathwayDocumentRequirementConfiguration : IEntityTypeConfiguration<PathwayDocumentRequirement>
{
    public void Configure(EntityTypeBuilder<PathwayDocumentRequirement> builder)
    {
        builder.HasKey(e => e.PathwayDocumentRequirementId);
        builder.HasOne(e => e.Pathway)
            .WithMany()
            .HasForeignKey(e => e.PathwayId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.DocumentType)
            .WithMany()
            .HasForeignKey(e => e.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.PathwayId, e.DocumentTypeId })
            .HasFilter("\"DeletedAt\" IS NULL")
            .IsUnique();
    }
}
