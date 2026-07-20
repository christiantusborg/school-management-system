using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Data.Configurations;

public class FacultyProfileSectionConfiguration : IEntityTypeConfiguration<FacultyProfileSection>
{
    public void Configure(EntityTypeBuilder<FacultyProfileSection> builder)
    {
        builder.HasKey(e => e.FacultyProfileSectionId);
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Kind).HasMaxLength(20).IsRequired();
    }
}

public class FacultyProfileFieldConfiguration : IEntityTypeConfiguration<FacultyProfileField>
{
    public void Configure(EntityTypeBuilder<FacultyProfileField> builder)
    {
        builder.HasKey(e => e.FacultyProfileFieldId);
        builder.Property(e => e.Label).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(20).IsRequired();
        builder.HasIndex(e => e.FacultyProfileSectionId);
    }
}

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.HasKey(e => e.TeacherId);
        builder.Property(e => e.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.UserId).HasMaxLength(450);
        builder.HasIndex(e => e.PartnerId);
        builder.HasIndex(e => e.UserId);
    }
}

public class TeacherProfileRowConfiguration : IEntityTypeConfiguration<TeacherProfileRow>
{
    public void Configure(EntityTypeBuilder<TeacherProfileRow> builder)
    {
        builder.HasKey(e => e.TeacherProfileRowId);
        builder.HasIndex(e => e.TeacherId);
    }
}

public class TeacherProfileValueConfiguration : IEntityTypeConfiguration<TeacherProfileValue>
{
    public void Configure(EntityTypeBuilder<TeacherProfileValue> builder)
    {
        builder.HasKey(e => e.TeacherProfileValueId);
        builder.Property(e => e.FileName).HasMaxLength(300);
        builder.HasIndex(e => new { e.TeacherProfileRowId, e.FacultyProfileFieldId }).IsUnique();
    }
}
