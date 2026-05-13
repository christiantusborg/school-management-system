using Odin.Api.Base.Data;

namespace SharedLibrary.Basics.Opaque.AdminApi.AdminUsers.V1.Endpoint;

[Route("/v1/admin/admin-users/{uid}")]
[EndpointTag("Admin.AdminUsers")]
public sealed class AdminUsersV1AdminDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(Route, EndpointHandlerAsync).RequireAuthorization("SuperAdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        string uid,
        HttpContext httpContext,
        [FromServices] OdinDbContext db,
        [FromServices] UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        var (currentUserId, authFail) = await AdminUsersHelpers.RequireSuperAdminAsync(httpContext, userManager);
        if (authFail is not null) return authFail;

        if (uid == currentUserId)
            return Results.BadRequest(new { error = "cannot_delete_self" });

        var (user, fail) = await AdminUsersHelpers.ResolveAdminAsync(uid, db, userManager, ct);
        if (fail is not null) return fail;

        user!.DeletedAt = DateTime.UtcNow;
        user.IsEnabled = false;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private const string Route = "/v1/admin/admin-users/{uid}";
}
