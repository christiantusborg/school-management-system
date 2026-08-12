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
    /// <summary>Phase-1 flag, kept in sync with <see cref="AccessLevel"/>
    /// (true when level == Edit). Reads treat absence of a row as "hidden".</summary>
    public bool Allowed { get; set; }
    /// <summary>Phase-2 access state: 0 Hidden · 1 NoAccess · 2 View · 3 Edit.</summary>
    public int AccessLevel { get; set; } = 3;
}

/// <summary>Per-role access to students in a given enrolment status (Phase 2
/// status-based access). Absence of a row = the role's status default.</summary>
public class RoleStatusAccess
{
    public Guid RoleStatusAccessId { get; set; } = Guid.NewGuid();
    public string RoleName { get; set; } = null!;
    public Guid StatusId { get; set; }
    /// <summary>0 Hidden (student filtered out) · 2 View (read-only) · 3 Edit (act).</summary>
    public int AccessLevel { get; set; } = 3;
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
    /// <summary>The item key (permission changes) or a status label (status changes).</summary>
    public string PermissionKey { get; set; } = null!;
    /// <summary>Set for status-grid changes; null for item changes.</summary>
    public Guid? StatusId { get; set; }
    public bool OldValue { get; set; }
    public bool NewValue { get; set; }
    /// <summary>Phase-2 access levels (0 Hidden · 1 NoAccess · 2 View · 3 Edit).</summary>
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
}
