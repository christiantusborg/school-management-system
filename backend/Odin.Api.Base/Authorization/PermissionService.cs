using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;

namespace Odin.Api.Base.Authorization;

/// <summary>
/// Resolves what an admin user is allowed to do from the configurable access
/// matrix. SuperAdministrator bypasses the matrix and always has every
/// permission; every other user's grants come from the <c>RolePermission</c>
/// table for their single level role.
/// </summary>
public interface IPermissionService
{
    bool IsSuperAdmin(ClaimsPrincipal user);

    /// <summary>The user's level role name, or null if not an admin.</summary>
    string? PickLevel(ClaimsPrincipal user);

    /// <summary>True if the user may perform the action identified by <paramref name="key"/>.</summary>
    Task<bool> HasAsync(ClaimsPrincipal user, string key, CancellationToken ct = default);

    /// <summary>All permission keys the user effectively holds (for the UI).</summary>
    Task<IReadOnlyList<string>> GetForUserAsync(ClaimsPrincipal user, CancellationToken ct = default);
}

public sealed class PermissionService(OdinDbContext db) : IPermissionService
{
    public bool IsSuperAdmin(ClaimsPrincipal user) => user.IsInRole(AdminLevels.SuperAdministrator);

    public string? PickLevel(ClaimsPrincipal user)
    {
        foreach (var level in AdminLevels.All)
            if (user.IsInRole(level)) return level;
        return null;
    }

    public async Task<bool> HasAsync(ClaimsPrincipal user, string key, CancellationToken ct = default)
    {
        if (IsSuperAdmin(user)) return true;               // Super bypasses the matrix.
        var level = PickLevel(user);
        if (level is null) return false;
        return await db.RolePermissions
            .AnyAsync(rp => rp.RoleName == level && rp.PermissionKey == key && rp.Allowed, ct);
    }

    public async Task<IReadOnlyList<string>> GetForUserAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (IsSuperAdmin(user))
            return AdminPermissions.Catalog.Select(p => p.Key).ToList();
        var level = PickLevel(user);
        if (level is null) return [];
        return await db.RolePermissions
            .Where(rp => rp.RoleName == level && rp.Allowed)
            .Select(rp => rp.PermissionKey)
            .ToListAsync(ct);
    }
}
