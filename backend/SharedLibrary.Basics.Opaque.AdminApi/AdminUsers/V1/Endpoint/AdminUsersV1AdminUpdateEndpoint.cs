using Odin.Api.Base.Authorization;
using Odin.Api.Base.Data;

namespace SharedLibrary.Basics.Opaque.AdminApi.AdminUsers.V1.Endpoint;

[Route("/v1/admin/admin-users/{uid}")]
[EndpointTag("Admin.AdminUsers")]
public sealed class AdminUsersV1AdminUpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch(Route, EndpointHandlerAsync).RequireAuthorization("SuperAdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        string uid,
        [FromBody] AdminUsersV1AdminUpdateRequest request,
        HttpContext httpContext,
        [FromServices] OdinDbContext db,
        [FromServices] UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        var (currentUserId, authFail) = await AdminUsersHelpers.RequireSuperAdminAsync(httpContext, userManager);
        if (authFail is not null) return authFail;

        var (user, fail) = await AdminUsersHelpers.ResolveAdminAsync(uid, db, userManager, ct);
        if (fail is not null) return fail;

        // Guard: a super-admin cannot demote or disable themselves — otherwise
        // they could accidentally lock the last super-admin out of the system.
        var isSelf = user!.Id == currentUserId;

        if (request.Email is not null)
        {
            var trimmed = request.Email.Trim();
            user.Email = trimmed;
            user.NormalizedEmail = userManager.NormalizeEmail(trimmed);
        }

        if (request.IsEnabled is not null)
        {
            if (isSelf && request.IsEnabled == false)
                return Results.BadRequest(new { error = "cannot_disable_self" });
            user.IsEnabled = request.IsEnabled.Value;
        }

        if (request.Level is not null)
        {
            if (!AdminLevels.IsValid(request.Level))
                return Results.BadRequest(new { error = "invalid_level" });

            if (isSelf && request.Level != AdminLevels.SuperAdministrator)
                return Results.BadRequest(new { error = "cannot_demote_self" });

            var currentRoles = await userManager.GetRolesAsync(user);
            var currentLevel = AdminUsersHelpers.PickLevel(currentRoles);
            if (currentLevel is not null && currentLevel != request.Level)
                await userManager.RemoveFromRoleAsync(user, currentLevel);
            if (currentLevel != request.Level)
                await userManager.AddToRoleAsync(user, request.Level);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { userId = user.Id });
    }

    private const string Route = "/v1/admin/admin-users/{uid}";
}

public sealed class AdminUsersV1AdminUpdateRequest
{
    public string? Email { get; init; }
    public bool? IsEnabled { get; init; }
    public string? Level { get; init; }
}
