using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;

namespace Odin.Api.Base.Authorization;

/// <summary>
/// Resolves what an admin user is allowed to do from the configurable access
/// matrix. SuperAdministrator bypasses the matrix and always has full access
/// (Edit everything); every other user's access comes from the
/// <c>RolePermission</c> (per item) and <c>RoleStatusAccess</c> (per status)
/// tables for their single level role.
/// </summary>
public interface IPermissionService
{
    bool IsSuperAdmin(ClaimsPrincipal user);
    string? PickLevel(ClaimsPrincipal user);

    /// <summary>Phase-1 boolean check: true when the item's level is Edit.</summary>
    Task<bool> HasAsync(ClaimsPrincipal user, string key, CancellationToken ct = default);

    /// <summary>Phase-2: the access level a user holds for an item.</summary>
    Task<AccessLevel> AccessAsync(ClaimsPrincipal user, string key, CancellationToken ct = default);

    /// <summary>Phase-2: the access level a user holds for an enrolment status.</summary>
    Task<AccessLevel> StatusAccessAsync(ClaimsPrincipal user, Guid statusId, CancellationToken ct = default);

    /// <summary>All item keys the user holds at Edit (drives Phase-1 auth.can).</summary>
    Task<IReadOnlyList<string>> GetForUserAsync(ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>Full item → level map for the UI (Super = Edit on everything).</summary>
    Task<IReadOnlyDictionary<string, int>> GetAccessMapAsync(ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>Status id → level map for the UI (Super = Edit; missing = Edit).</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetStatusMapAsync(ClaimsPrincipal user, CancellationToken ct = default);
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
        => await AccessAsync(user, key, ct) == AccessLevel.Edit;

    public async Task<AccessLevel> AccessAsync(ClaimsPrincipal user, string key, CancellationToken ct = default)
    {
        if (IsSuperAdmin(user)) return AccessLevel.Edit;   // Super bypasses.
        var level = PickLevel(user);
        if (level is null) return AccessLevel.Hidden;
        var row = await db.RolePermissions
            .Where(rp => rp.RoleName == level && rp.PermissionKey == key)
            .Select(rp => (int?)rp.AccessLevel)
            .FirstOrDefaultAsync(ct);
        return row is { } v && AccessLevels.IsValid(v) ? (AccessLevel)v : AccessLevel.Hidden;
    }

    public async Task<AccessLevel> StatusAccessAsync(ClaimsPrincipal user, Guid statusId, CancellationToken ct = default)
    {
        if (IsSuperAdmin(user)) return AccessLevel.Edit;
        var level = PickLevel(user);
        if (level is null) return AccessLevel.Hidden;
        var row = await db.RoleStatusAccesses
            .Where(rp => rp.RoleName == level && rp.StatusId == statusId)
            .Select(rp => (int?)rp.AccessLevel)
            .FirstOrDefaultAsync(ct);
        // Missing status row defaults to Edit (fail-open on visibility): a status
        // is only restricted once an admin explicitly sets a lower level.
        return row is { } v && AccessLevels.IsValid(v) ? (AccessLevel)v : AccessLevel.Edit;
    }

    public async Task<IReadOnlyList<string>> GetForUserAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (IsSuperAdmin(user)) return AdminResources.Catalog.Select(r => r.Key).ToList();
        var level = PickLevel(user);
        if (level is null) return [];
        return await db.RolePermissions
            .Where(rp => rp.RoleName == level && rp.AccessLevel == (int)AccessLevel.Edit)
            .Select(rp => rp.PermissionKey)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetAccessMapAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (IsSuperAdmin(user))
            return AdminResources.Catalog.ToDictionary(r => r.Key, _ => (int)AccessLevel.Edit);
        var level = PickLevel(user);
        if (level is null) return new Dictionary<string, int>();
        return await db.RolePermissions
            .Where(rp => rp.RoleName == level)
            .ToDictionaryAsync(rp => rp.PermissionKey, rp => rp.AccessLevel, ct);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetStatusMapAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (IsSuperAdmin(user)) return new Dictionary<Guid, int>();   // Super: all Edit (client treats missing as Edit)
        var level = PickLevel(user);
        if (level is null) return new Dictionary<Guid, int>();
        return await db.RoleStatusAccesses
            .Where(rp => rp.RoleName == level)
            .ToDictionaryAsync(rp => rp.StatusId, rp => rp.AccessLevel, ct);
    }
}
