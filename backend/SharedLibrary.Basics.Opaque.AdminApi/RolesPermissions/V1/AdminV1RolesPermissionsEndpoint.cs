using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.AdminApi.AdminUsers.V1;
using SharedLibrary.Basics.Opaque.Domains.Authorization;

namespace SharedLibrary.Basics.Opaque.AdminApi.RolesPermissions.V1;

/// <summary>
/// The configurable access matrix (Phase 2). SuperAdministrator-only: read the
/// matrix (roles × the resource catalogue with a 4-state level per cell, plus
/// the per-status grid), set a single item or status level (append-only audit),
/// and read the change history. SuperAdministrator itself is never editable — it
/// bypasses the matrix and always has Edit on everything.
/// </summary>
[Route("/v1/admin/roles-permissions")]
[EndpointTag("Admin.RolesPermissions")]
public sealed class AdminV1RolesPermissionsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/roles-permissions/matrix", MatrixAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/roles-permissions/grant", GrantAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/roles-permissions/status-grant", StatusGrantAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/roles-permissions/audit", AuditAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/my-permissions", MyPermissionsAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class GrantRequest
    {
        public string? RoleName { get; init; }
        public string? PermissionKey { get; init; }
        public int AccessLevel { get; init; }
    }
    public sealed class StatusGrantRequest
    {
        public string? RoleName { get; init; }
        public Guid StatusId { get; init; }
        public int AccessLevel { get; init; }
    }

    private static async Task<IResult> MatrixAsync(
        HttpContext http, OdinDbContext db, UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var (_, fail) = await AdminUsersHelpers.RequireSuperAdminAsync(http, userManager);
        if (fail is not null) return fail;

        var roles = await db.AdminRoles.OrderBy(r => r.SortOrder).ToListAsync(ct);
        var perms = await db.RolePermissions.ToListAsync(ct);
        var itemGrants = perms.GroupBy(g => g.RoleName)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.PermissionKey, x => x.AccessLevel));

        var statuses = await db.EnrollmentStatuses
            .Where(s => s.DeletedAt == null)
            .OrderBy(s => s.Level)
            .Select(s => new { id = s.EnrollmentStatusId, s.Code, s.Name, s.Level, s.NextActionRole })
            .ToListAsync(ct);
        var statusRows = await db.RoleStatusAccesses.ToListAsync(ct);
        var statusGrants = statusRows.GroupBy(g => g.RoleName)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.StatusId, x => x.AccessLevel));

        return Results.Ok(new
        {
            superRole = AdminLevels.SuperAdministrator,
            roles = roles.Select(r => new { r.Name, r.Label, r.Description, r.IsSystem, r.SortOrder }),
            resources = AdminResources.Catalog.Select(r => new
            {
                r.Key, r.Surface, r.Group, r.Label, r.Description,
                type = r.Type.ToString().ToLowerInvariant(),
            }),
            itemGrants,
            statuses,
            statusGrants,
        });
    }

    private static async Task<IResult> GrantAsync(
        [FromBody] GrantRequest body, HttpContext http, OdinDbContext db,
        UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var (callerId, fail) = await AdminUsersHelpers.RequireSuperAdminAsync(http, userManager);
        if (fail is not null) return fail;

        var role = body.RoleName;
        var key = body.PermissionKey;
        if (string.IsNullOrWhiteSpace(role) || !AdminLevels.IsValid(role)) return Results.BadRequest(new { error = "invalid_role" });
        if (role == AdminLevels.SuperAdministrator) return Results.BadRequest(new { error = "super_admin_not_editable" });
        if (string.IsNullOrWhiteSpace(key) || !AdminResources.IsValidKey(key)) return Results.BadRequest(new { error = "invalid_permission" });
        if (!AccessLevels.IsValid(body.AccessLevel)) return Results.BadRequest(new { error = "invalid_level" });

        var existing = await db.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleName == role && rp.PermissionKey == key, ct);
        var oldLevel = existing?.AccessLevel ?? 0;
        if (oldLevel == body.AccessLevel) return Results.Ok(new { unchanged = true });

        if (existing is null)
            db.RolePermissions.Add(new RolePermission { RoleName = role!, PermissionKey = key!, AccessLevel = body.AccessLevel, Allowed = body.AccessLevel == (int)AccessLevel.Edit });
        else { existing.AccessLevel = body.AccessLevel; existing.Allowed = body.AccessLevel == (int)AccessLevel.Edit; }

        await WriteAuditAsync(db, userManager, callerId!, role!, key!, null, oldLevel, body.AccessLevel, ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { roleName = role, permissionKey = key, accessLevel = body.AccessLevel });
    }

    private static async Task<IResult> StatusGrantAsync(
        [FromBody] StatusGrantRequest body, HttpContext http, OdinDbContext db,
        UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var (callerId, fail) = await AdminUsersHelpers.RequireSuperAdminAsync(http, userManager);
        if (fail is not null) return fail;

        var role = body.RoleName;
        if (string.IsNullOrWhiteSpace(role) || !AdminLevels.IsValid(role)) return Results.BadRequest(new { error = "invalid_role" });
        if (role == AdminLevels.SuperAdministrator) return Results.BadRequest(new { error = "super_admin_not_editable" });
        if (!AccessLevels.IsValid(body.AccessLevel)) return Results.BadRequest(new { error = "invalid_level" });
        var status = await db.EnrollmentStatuses.FirstOrDefaultAsync(s => s.EnrollmentStatusId == body.StatusId && s.DeletedAt == null, ct);
        if (status is null) return Results.BadRequest(new { error = "invalid_status" });

        var existing = await db.RoleStatusAccesses.FirstOrDefaultAsync(r => r.RoleName == role && r.StatusId == body.StatusId, ct);
        var oldLevel = existing?.AccessLevel ?? (int)AccessLevel.Edit;
        if (oldLevel == body.AccessLevel) return Results.Ok(new { unchanged = true });

        if (existing is null)
            db.RoleStatusAccesses.Add(new RoleStatusAccess { RoleName = role!, StatusId = body.StatusId, AccessLevel = body.AccessLevel });
        else existing.AccessLevel = body.AccessLevel;

        await WriteAuditAsync(db, userManager, callerId!, role!, "status:" + status.Code, body.StatusId, oldLevel, body.AccessLevel, ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { roleName = role, statusId = body.StatusId, accessLevel = body.AccessLevel });
    }

    private static async Task WriteAuditAsync(
        OdinDbContext db, UserManager<ApplicationUser> userManager, string callerId,
        string role, string key, Guid? statusId, int oldLevel, int newLevel, CancellationToken ct)
    {
        var callerName = (await userManager.FindByIdAsync(callerId))?.UserName;
        db.PermissionAuditLogs.Add(new PermissionAuditLog
        {
            ChangedByUserId = callerId,
            ChangedByUsername = callerName,
            RoleName = role,
            PermissionKey = key,
            StatusId = statusId,
            OldLevel = oldLevel,
            NewLevel = newLevel,
            OldValue = oldLevel == (int)AccessLevel.Edit,
            NewValue = newLevel == (int)AccessLevel.Edit,
        });
    }

    private static async Task<IResult> MyPermissionsAsync(
        HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        var map = await perms.GetAccessMapAsync(http.User, ct);
        var statusMap = await perms.GetStatusMapAsync(http.User, ct);
        var edit = await perms.GetForUserAsync(http.User, ct);
        return Results.Ok(new
        {
            isSuperAdmin = perms.IsSuperAdmin(http.User),
            permissions = edit,                          // Phase-1 auth.can (Edit keys)
            access = map,                                // key -> level
            statusAccess = statusMap.ToDictionary(k => k.Key.ToString(), v => v.Value),
        });
    }

    private static async Task<IResult> AuditAsync(
        HttpContext http, OdinDbContext db, UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var (_, fail) = await AdminUsersHelpers.RequireSuperAdminAsync(http, userManager);
        if (fail is not null) return fail;

        var items = await db.PermissionAuditLogs
            .OrderByDescending(a => a.ChangedAt)
            .Take(200)
            .Select(a => new { a.ChangedAt, changedBy = a.ChangedByUsername, a.RoleName, a.PermissionKey, a.OldLevel, a.NewLevel })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }
}
