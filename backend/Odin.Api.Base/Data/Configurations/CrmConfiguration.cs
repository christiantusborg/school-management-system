using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Crm;

namespace Odin.Api.Base.Data.Configurations;

public class CrmPipelineConfiguration : IEntityTypeConfiguration<CrmPipeline>
{
    public void Configure(EntityTypeBuilder<CrmPipeline> builder)
    {
        builder.HasKey(e => e.CrmPipelineId);
        builder.Property(e => e.Name).HasMaxLength(200);
    }
}

public class CrmStageConfiguration : IEntityTypeConfiguration<CrmStage>
{
    public void Configure(EntityTypeBuilder<CrmStage> builder)
    {
        builder.HasKey(e => e.CrmStageId);
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Color).HasMaxLength(20);
        builder.HasOne(e => e.Pipeline)
            .WithMany()
            .HasForeignKey(e => e.CrmPipelineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CrmLeadSourceConfiguration : IEntityTypeConfiguration<CrmLeadSource>
{
    public void Configure(EntityTypeBuilder<CrmLeadSource> builder)
    {
        builder.HasKey(e => e.CrmLeadSourceId);
        builder.Property(e => e.Name).HasMaxLength(200);
    }
}

public class CrmLeadConfiguration : IEntityTypeConfiguration<CrmLead>
{
    public void Configure(EntityTypeBuilder<CrmLead> builder)
    {
        builder.HasKey(e => e.CrmLeadId);
        builder.Property(e => e.Name).HasMaxLength(300);
        builder.Property(e => e.Email).HasMaxLength(320);
        builder.Property(e => e.Phone).HasMaxLength(60);
        builder.Property(e => e.Country).HasMaxLength(120);
        builder.Property(e => e.Note).HasMaxLength(4000);
        builder.Property(e => e.LostReason).HasMaxLength(1000);
        builder.HasOne(e => e.Pipeline)
            .WithMany()
            .HasForeignKey(e => e.CrmPipelineId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Stage)
            .WithMany()
            .HasForeignKey(e => e.CrmStageId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Source)
            .WithMany()
            .HasForeignKey(e => e.CrmLeadSourceId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(e => e.CrmPipelineId);
        builder.HasIndex(e => e.AssignedToUserId);
        builder.HasIndex(e => e.Email);
    }
}

public class CrmActivityConfiguration : IEntityTypeConfiguration<CrmActivity>
{
    public void Configure(EntityTypeBuilder<CrmActivity> builder)
    {
        builder.HasKey(e => e.CrmActivityId);
        builder.Property(e => e.Body).HasMaxLength(4000);
        builder.HasOne(e => e.Lead)
            .WithMany(l => l.Activities)
            .HasForeignKey(e => e.CrmLeadId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.CrmLeadId, e.DueAt });
    }
}

public class CrmAssignmentConfigConfiguration : IEntityTypeConfiguration<CrmAssignmentConfig>
{
    public void Configure(EntityTypeBuilder<CrmAssignmentConfig> builder)
    {
        builder.HasKey(e => e.CrmAssignmentConfigId);
        builder.Property(e => e.CrmAssignmentConfigId).ValueGeneratedNever();
        builder.Property(e => e.MemberUserIds).HasMaxLength(4000);
    }
}
