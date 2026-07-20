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

public class ModuleCohortSettingsConfiguration : IEntityTypeConfiguration<ModuleCohortSettings>
{
    public void Configure(EntityTypeBuilder<ModuleCohortSettings> builder)
    {
        builder.HasKey(e => e.ModuleCohortSettingsId);
        builder.Property(e => e.CohortNumberPattern).HasMaxLength(200).IsRequired();
    }
}
