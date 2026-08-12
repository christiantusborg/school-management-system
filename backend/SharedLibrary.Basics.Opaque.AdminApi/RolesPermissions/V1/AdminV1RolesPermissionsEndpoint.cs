using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.AdminApi.AdminUsers.V1;
using SharedLibrary.Basics.Opaque.Domains.Authorization;

namespace SharedLibrary.Basics.Opaque.AdminApi.RolesPermissions.V1;

/// <summary>
/// The configurable access matrix. SuperAdministrator-only: read the matrix
/// (roles × permission catalogue + current grants), toggle a single grant
/// (append-only audit log), and read the change history. SuperAdministrator
/// itself is never editable — it bypasses the matrix and always has everything.
/// </summary>
[Route("/v1/admin/roles-permissions")]
[EndpointTag("Admin.RolesPermissions")]
public sealed class AdminV1RolesPermissionsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/roles-permissions/matrix", MatrixAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/roles-permissions/grant", GrantAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/roles-permissions/audit", AuditAsync).RequireAuthorization("AdminOnly");
        // Any admin reads their own effective permissions (drives UI gating).
        app.MapGet("/v1/admin/my-permissions", MyPermissionsAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class GrantRequest
    {
        public string? RoleName { get; init; }
        public string? PermissionKey { get; init; }
        public bool Allowed { get; init; }
    }

    private static async Task<IResult> MatrixAsync(
        HttpContext http, OdinDbContext db, UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var (_, fail) = await AdminUsersHelpers.RequireSuperAdminAsync(http, userManager);
        if (fail is not null) return fail;

        var roles = await db.AdminRoles.OrderBy(r => r.SortOrder).ToListAsync(ct);
        var grants = await db.RolePermissions.Where(rp => rp.Allowed).ToListAsync(ct);
        var byRole = grants.GroupBy(g => g.RoleName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PermissionKey).ToArray());

        return Results.Ok(new
        {
            superRole = AdminLevels.SuperAdministrator,
            roles = roles.Select(r => new { r.Name, r.Label, r.Description, r.IsSystem, r.SortOrder }),
            permissions = AdminPermissions.Catalog.Select(p => new { p.Key, p.Area, p.Label, p.Description }),
            grants = byRole,
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
        if (string.IsNullOrWhiteSpace(role) || !AdminLevels.IsValid(role))
            return Results.BadRequest(new { error = "invalid_role" });
        if (role == AdminLevels.SuperAdministrator)
            return Results.BadRequest(new { error = "super_admin_not_editable" });
        if (string.IsNullOrWhiteSpace(key) || !AdminPermissions.IsValidKey(key))
            return Results.BadRequest(new { error = "invalid_permission" });

        var existing = await db.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleName == role && rp.PermissionKey == key, ct);
        var oldValue = existing?.Allowed ?? false;
        if (oldValue == body.Allowed) return Results.Ok(new { unchanged = true });

        if (existing is null)
            db.RolePermissions.Add(new RolePermission { RoleName = role!, PermissionKey = key!, Allowed = body.Allowed });
        else
            existing.Allowed = body.Allowed;

        var callerName = (await userManager.FindByIdAsync(callerId!))?.UserName;
        db.PermissionAuditLogs.Add(new PermissionAuditLog
        {
            ChangedByUserId = callerId,
            ChangedByUsername = callerName,
            RoleName = role!,
            PermissionKey = key!,
            OldValue = oldValue,
            NewValue = body.Allowed,
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { roleName = role, permissionKey = key, allowed = body.Allowed });
    }

    private static async Task<IResult> MyPermissionsAsync(
        HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        // RolePathGuardMiddleware already guarantees an Admin reached /v1/admin/.
        var keys = await perms.GetForUserAsync(http.User, ct);
        return Results.Ok(new { isSuperAdmin = perms.IsSuperAdmin(http.User), permissions = keys });
    }

    private static async Task<IResult> AuditAsync(
        HttpContext http, OdinDbContext db, UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var (_, fail) = await AdminUsersHelpers.RequireSuperAdminAsync(http, userManager);
        if (fail is not null) return fail;

        var items = await db.PermissionAuditLogs
            .OrderByDescending(a => a.ChangedAt)
            .Take(200)
            .Select(a => new
            {
                a.ChangedAt,
                changedBy = a.ChangedByUsername,
                a.RoleName,
                a.PermissionKey,
                a.OldValue,
                a.NewValue,
            })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }
}
