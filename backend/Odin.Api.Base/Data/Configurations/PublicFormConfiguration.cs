using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace Odin.Api.Base.Data.Configurations;

public class PublicFormConfiguration : IEntityTypeConfiguration<PublicForm>
{
    public void Configure(EntityTypeBuilder<PublicForm> builder)
    {
        builder.HasKey(e => e.PublicFormId);
        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Slug).HasMaxLength(64).IsRequired();
        builder.HasIndex(e => e.Slug).IsUnique();
        builder.Property(e => e.Currency).HasMaxLength(8).IsRequired();
        builder.Property(e => e.CreatedByUserId).HasMaxLength(450);
        // Per-form owner reference values, stored as queryable jsonb (Npgsql).
        builder.Property(e => e.OwnerReferencesJson).HasColumnType("jsonb");

        builder.HasOne(e => e.QuestionnaireTemplate)
            .WithMany()
            .HasForeignKey(e => e.QuestionnaireTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.DocumentTemplate)
            .WithMany()
            .HasForeignKey(e => e.DocumentTemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PublicFormSubmissionConfiguration : IEntityTypeConfiguration<PublicFormSubmission>
{
    public void Configure(EntityTypeBuilder<PublicFormSubmission> builder)
    {
        builder.HasKey(e => e.PublicFormSubmissionId);
        builder.HasIndex(e => e.PublicFormId);
        // Plain text by explicit decision: no field/KEM encryption in intake.
        builder.Property(e => e.AnswersJson).HasColumnType("text").IsRequired();
        builder.Property(e => e.QuestionnaireVersionHash).HasMaxLength(64);
        builder.Property(e => e.RespondentEmail).HasMaxLength(256);
        builder.Property(e => e.RespondentName).HasMaxLength(256);

        builder.HasOne(e => e.PublicForm)
            .WithMany()
            .HasForeignKey(e => e.PublicFormId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PublicFormPaymentConfiguration : IEntityTypeConfiguration<PublicFormPayment>
{
    public void Configure(EntityTypeBuilder<PublicFormPayment> builder)
    {
        builder.HasKey(e => e.PublicFormPaymentId);
        builder.HasIndex(e => e.PublicFormSubmissionId);
        builder.Property(e => e.Currency).HasMaxLength(8).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
        builder.Property(e => e.ProviderReference).HasMaxLength(256);

        builder.HasOne(e => e.PublicFormSubmission)
            .WithMany()
            .HasForeignKey(e => e.PublicFormSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
