namespace SharedLibrary.Basics.Opaque.Domains.Authorization;

/// <summary>
/// Metadata for an admin role (privilege level). The <see cref="Name"/> matches
/// the ASP.NET Identity role name, so user↔role assignment is unchanged. This
/// table exists so roles are stored as data — the six built-in roles are seeded
/// with <see cref="IsSystem"/> = true; custom roles (a later phase) are just new
/// rows.
/// </summary>
public class AdminRole
{
    /// <summary>Identity role name, e.g. "Administrator". Primary key.</summary>
    public string Name { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string? Description { get; set; }
    /// <summary>True for the six built-in roles (cannot be deleted).</summary>
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// A single grant: whether <see cref="RoleName"/> holds
/// <see cref="PermissionKey"/> (a key from the code-defined permission
/// catalogue). Absence of a row means "not granted". SuperAdministrator is never
/// stored here — it bypasses the matrix and always has every permission.
/// </summary>
public class RolePermission
{
    public Guid RolePermissionId { get; set; } = Guid.NewGuid();
    public string RoleName { get; set; } = null!;
    public string PermissionKey { get; set; } = null!;
    public bool Allowed { get; set; }
}

/// <summary>
/// Append-only audit record of a change to the access matrix: who changed which
/// role's permission, when, and from → to.
/// </summary>
public class PermissionAuditLog
{
    public Guid PermissionAuditLogId { get; set; } = Guid.NewGuid();
    public string? ChangedByUserId { get; set; }
    public string? ChangedByUsername { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string RoleName { get; set; } = null!;
    public string PermissionKey { get; set; } = null!;
    public bool OldValue { get; set; }
    public bool NewValue { get; set; }
}
