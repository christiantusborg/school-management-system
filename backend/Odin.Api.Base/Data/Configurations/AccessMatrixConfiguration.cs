using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedLibrary.Basics.Opaque.Domains.Authorization;

namespace Odin.Api.Base.Data.Configurations;

public class AdminRoleConfiguration : IEntityTypeConfiguration<AdminRole>
{
    public void Configure(EntityTypeBuilder<AdminRole> builder)
    {
        builder.HasKey(e => e.Name);
        builder.Property(e => e.Name).HasMaxLength(64);
        builder.Property(e => e.Label).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(512);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(e => e.RolePermissionId);
        builder.Property(e => e.RoleName).HasMaxLength(64).IsRequired();
        builder.Property(e => e.PermissionKey).HasMaxLength(128).IsRequired();
        builder.HasIndex(e => new { e.RoleName, e.PermissionKey }).IsUnique();
    }
}

public class RoleStatusAccessConfiguration : IEntityTypeConfiguration<RoleStatusAccess>
{
    public void Configure(EntityTypeBuilder<RoleStatusAccess> builder)
    {
        builder.HasKey(e => e.RoleStatusAccessId);
        builder.Property(e => e.RoleName).HasMaxLength(64).IsRequired();
        builder.HasIndex(e => new { e.RoleName, e.StatusId }).IsUnique();
    }
}

public class PermissionAuditLogConfiguration : IEntityTypeConfiguration<PermissionAuditLog>
{
    public void Configure(EntityTypeBuilder<PermissionAuditLog> builder)
    {
        builder.HasKey(e => e.PermissionAuditLogId);
        builder.Property(e => e.ChangedByUserId).HasMaxLength(450);
        builder.Property(e => e.ChangedByUsername).HasMaxLength(256);
        builder.Property(e => e.RoleName).HasMaxLength(64).IsRequired();
        builder.Property(e => e.PermissionKey).HasMaxLength(128).IsRequired();
        builder.HasIndex(e => e.ChangedAt);
    }
}
