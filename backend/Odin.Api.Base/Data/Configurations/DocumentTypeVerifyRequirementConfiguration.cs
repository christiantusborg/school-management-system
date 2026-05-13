using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Data.Configurations;

public class DocumentTypeVerifyRequirementConfiguration : IEntityTypeConfiguration<DocumentTypeVerifyRequirement>
{
    public void Configure(EntityTypeBuilder<DocumentTypeVerifyRequirement> builder)
    {
        builder.HasKey(e => e.DocumentTypeVerifyRequirementId);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.RejectionLabel).HasMaxLength(200);
        builder.HasIndex(e => new { e.DocumentTypeId, e.Name });
        builder.HasOne(e => e.DocumentType)
            .WithMany(t => t.DocumentTypeVerifyRequirements)
            .HasForeignKey(e => e.DocumentTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
