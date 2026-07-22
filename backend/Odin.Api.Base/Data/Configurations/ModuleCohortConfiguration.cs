using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class ModuleCohortConfiguration : IEntityTypeConfiguration<ModuleCohort>
{
    public void Configure(EntityTypeBuilder<ModuleCohort> builder)
    {
        builder.HasKey(e => e.ModuleCohortId);
        builder.Property(e => e.CohortNumber).HasMaxLength(200).IsRequired();
        builder.HasIndex(e => e.PartnerId);
        builder.HasIndex(e => e.SubjectId);
        builder.HasIndex(e => e.TeacherId);
    }
}

public class ModuleCohortStudentConfiguration : IEntityTypeConfiguration<ModuleCohortStudent>
{
    public void Configure(EntityTypeBuilder<ModuleCohortStudent> builder)
    {
        builder.HasKey(e => e.ModuleCohortStudentId);
        builder.HasIndex(e => e.ModuleCohortId);
        builder.HasIndex(e => e.StudentEnrollmentId);
    }
}

public class CohortUploadFieldConfiguration : IEntityTypeConfiguration<CohortUploadField>
{
    public void Configure(EntityTypeBuilder<CohortUploadField> builder)
    {
        builder.HasKey(e => e.CohortUploadFieldId);
        builder.Property(e => e.Label).HasMaxLength(300).IsRequired();
    }
}

public class CohortUploadFileConfiguration : IEntityTypeConfiguration<CohortUploadFile>
{
    public void Configure(EntityTypeBuilder<CohortUploadFile> builder)
    {
        builder.HasKey(e => e.CohortUploadFileId);
        builder.Property(e => e.FileName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.StoragePath).HasMaxLength(600).IsRequired();
        builder.HasIndex(e => e.ModuleCohortId);
    }
}

public class ModuleCohortQuestionnaireConfiguration : IEntityTypeConfiguration<ModuleCohortQuestionnaire>
{
    public void Configure(EntityTypeBuilder<ModuleCohortQuestionnaire> builder)
    {
        builder.HasKey(e => e.ModuleCohortQuestionnaireId);
        builder.HasIndex(e => e.ModuleCohortId);
    }
}

public class CohortQuestionnaireResponseConfiguration : IEntityTypeConfiguration<CohortQuestionnaireResponse>
{
    public void Configure(EntityTypeBuilder<CohortQuestionnaireResponse> builder)
    {
        builder.HasKey(e => e.CohortQuestionnaireResponseId);
        builder.Property(e => e.QuestionnaireVersionHash).HasMaxLength(64);
        builder.HasIndex(e => e.ModuleCohortQuestionnaireId);
    }
}

public class CohortQuestionnaireCompletionConfiguration : IEntityTypeConfiguration<CohortQuestionnaireCompletion>
{
    public void Configure(EntityTypeBuilder<CohortQuestionnaireCompletion> builder)
    {
        builder.HasKey(e => e.CohortQuestionnaireCompletionId);
        // One completion per student per questionnaire; also the gate lookup.
        builder.HasIndex(e => new { e.ModuleCohortQuestionnaireId, e.StudentEnrollmentId }).IsUnique();
        builder.HasIndex(e => e.StudentEnrollmentId);
    }
}

public class CohortTypeConfiguration : IEntityTypeConfiguration<CohortType>
{
    public void Configure(EntityTypeBuilder<CohortType> builder)
    {
        builder.HasKey(e => e.CohortTypeId);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
    }
}

public class CohortTypeFieldConfiguration : IEntityTypeConfiguration<CohortTypeField>
{
    public void Configure(EntityTypeBuilder<CohortTypeField> builder)
    {
        builder.HasKey(e => e.CohortTypeFieldId);
        builder.Property(e => e.Label).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(20).IsRequired();
        builder.HasIndex(e => e.CohortTypeId);
    }
}

public class CohortFieldValueConfiguration : IEntityTypeConfiguration<CohortFieldValue>
{
    public void Configure(EntityTypeBuilder<CohortFieldValue> builder)
    {
        builder.HasKey(e => e.CohortFieldValueId);
        builder.Property(e => e.FileName).HasMaxLength(300);
        builder.HasIndex(e => new { e.ModuleCohortId, e.CohortTypeFieldId }).IsUnique();
    }
}

public class ModuleCohortSettingsConfiguration : IEntityTypeConfiguration<ModuleCohortSettings>
{
    public void Configure(EntityTypeBuilder<ModuleCohortSettings> builder)
    {
        builder.HasKey(e => e.ModuleCohortSettingsId);
        builder.Property(e => e.CohortNumberPattern).HasMaxLength(200).IsRequired();
    }
}
