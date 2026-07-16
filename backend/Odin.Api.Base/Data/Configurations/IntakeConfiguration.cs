using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace Odin.Api.Base.Data.Configurations;

public class IntakeInstanceConfiguration : IEntityTypeConfiguration<IntakeInstance>
{
    public void Configure(EntityTypeBuilder<IntakeInstance> builder)
    {
        builder.HasKey(e => e.IntakeInstanceId);
        builder.Property(e => e.Name).HasMaxLength(256);
        builder.Property(e => e.Audience).HasMaxLength(32).IsRequired();
        builder.Property(e => e.InlineDefinitionJson).HasColumnType("text");
        builder.Property(e => e.OutputProfileJson).HasColumnType("text").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasMaxLength(450);
        builder.HasIndex(e => e.Audience);

        builder.HasOne(e => e.QuestionnaireTemplate)
            .WithMany()
            .HasForeignKey(e => e.QuestionnaireTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class IntakeResponseConfiguration : IEntityTypeConfiguration<IntakeResponse>
{
    public void Configure(EntityTypeBuilder<IntakeResponse> builder)
    {
        builder.HasKey(e => e.IntakeResponseId);
        builder.Property(e => e.QuestionnaireVersionHash).HasMaxLength(64);
        builder.Property(e => e.CreatedByUserId).HasMaxLength(450);
        builder.HasIndex(e => e.IntakeInstanceId);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.PartnerId);

        // Plain text by explicit decision: no field/KEM encryption anywhere in
        // the intake feature (user call, 2026-07-15). Survey answers are not
        // in the same sensitivity class as OPRF seeds or SMTP credentials.
        builder.Property(e => e.AnswersJson)
            .HasColumnType("text")
            .IsRequired();

        builder.HasOne(e => e.IntakeInstance)
            .WithMany()
            .HasForeignKey(e => e.IntakeInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
