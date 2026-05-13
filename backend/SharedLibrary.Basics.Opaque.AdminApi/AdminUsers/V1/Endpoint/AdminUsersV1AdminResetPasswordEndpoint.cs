using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using Odin.Api.Base.Services;

namespace SharedLibrary.Basics.Opaque.AdminApi.AdminUsers.V1.Endpoint;

[Route("/v1/admin/admin-users/{uid}/reset-password")]
[EndpointTag("Admin.AdminUsers")]
public sealed class AdminUsersV1AdminResetPasswordEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync).RequireAuthorization("SuperAdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        string uid,
        HttpContext httpContext,
        [FromServices] OdinDbContext db,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] OpaqueUserCreationService creator,
        CancellationToken ct)
    {
        var (_, authFail) = await AdminUsersHelpers.RequireSuperAdminAsync(httpContext, userManager);
        if (authFail is not null) return authFail;

        var (user, fail) = await AdminUsersHelpers.ResolveAdminAsync(uid, db, userManager, ct);
        if (fail is not null) return fail;

        var credentials = await db.OpaqueCredentials.Where(c => c.UserId == uid).ToListAsync(ct);
        db.OpaqueCredentials.RemoveRange(credentials);

        var kemKeys = await db.KemKeyPairs.Where(k => k.UserId == uid).ToListAsync(ct);
        db.KemKeyPairs.RemoveRange(kemKeys);

        var recoveryCodes = await db.OpaqueRecoveryCodes.Where(r => r.UserId == uid).ToListAsync(ct);
        db.OpaqueRecoveryCodes.RemoveRange(recoveryCodes);

        await db.SaveChangesAsync(ct);

        var password = await creator.RegenerateCredentialsAsync(user!, ct);

        return Results.Ok(new
        {
            userId = user!.Id,
            username = user.UserName,
            temporaryPassword = password,
        });
    }

    private const string Route = "/v1/admin/admin-users/{uid}/reset-password";
}
