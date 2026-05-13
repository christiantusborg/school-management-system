using System.Linq;
using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Data;

namespace SharedLibrary.Basics.Opaque.AdminApi.AdminUsers.V1;

public static class AdminUsersHelpers
{
    /// <summary>
    /// Extracts the first admin level role (from <see cref="AdminLevels.All"/>)
    /// from a list of role names. Returns null if none matched.
    /// </summary>
    public static string? PickLevel(IEnumerable<string> roles) =>
        roles.FirstOrDefault(r => AdminLevels.IsValid(r));

    /// <summary>
    /// Guards an endpoint handler: requires an authenticated user in the
    /// <see cref="AdminLevels.SuperAdministrator"/> role. Returns null on
    /// success, or an <see cref="IResult"/> with 401/403 on failure.
    /// The parent MapGroup in Program.cs applies AllowAnonymous, so the
    /// ASP.NET <c>RequireAuthorization</c> convention is effectively a no-op
    /// and we enforce auth manually here (matches the partner/my-users pattern).
    /// </summary>
    public static async Task<(string? callerUserId, IResult? fail)> RequireSuperAdminAsync(
        HttpContext httpContext, UserManager<ApplicationUser> userManager)
    {
        var callerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return (null, Results.Unauthorized());

        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled)
            return (null, Results.Unauthorized());

        if (!await userManager.IsInRoleAsync(caller, AdminLevels.SuperAdministrator))
            return (null, Results.Forbid());

        return (callerId, null);
    }

    /// <summary>
    /// Lighter guard for endpoints that only need any authenticated Admin.
    /// </summary>
    public static async Task<(string? callerUserId, IResult? fail)> RequireAdminAsync(
        HttpContext httpContext, UserManager<ApplicationUser> userManager)
    {
        var callerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return (null, Results.Unauthorized());

        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled)
            return (null, Results.Unauthorized());

        if (!await userManager.IsInRoleAsync(caller, "Admin"))
            return (null, Results.Forbid());

        return (callerId, null);
    }

    /// <summary>
    /// Resolves an admin user by id and verifies they are (a) in the Admin
    /// role and (b) not soft-deleted. Returns 404 otherwise.
    /// </summary>
    public static async Task<(ApplicationUser? user, IResult? fail)> ResolveAdminAsync(
        string userId, OdinDbContext db, UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct);
        if (user is null) return (null, Results.NotFound());
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains("Admin")) return (null, Results.NotFound());
        return (user, null);
    }
}
