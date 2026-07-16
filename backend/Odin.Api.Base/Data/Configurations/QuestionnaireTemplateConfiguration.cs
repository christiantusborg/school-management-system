using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace Odin.Api.Base.Data.Configurations;

public class QuestionnaireTemplateConfiguration : IEntityTypeConfiguration<QuestionnaireTemplate>
{
    public void Configure(EntityTypeBuilder<QuestionnaireTemplate> builder)
    {
        builder.HasKey(e => e.QuestionnaireTemplateId);
        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Version).HasMaxLength(32).IsRequired();
        builder.Property(e => e.DefinitionJson).HasColumnType("text").IsRequired();
        // SHA-256 as uppercase hex is exactly 64 chars.
        builder.Property(e => e.DefinitionHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.CreatedByUserId).HasMaxLength(450);
        builder.HasIndex(e => e.Name);
    }
}
