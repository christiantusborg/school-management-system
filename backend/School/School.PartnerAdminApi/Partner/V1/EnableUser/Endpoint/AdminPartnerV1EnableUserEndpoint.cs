using Odin.Api.Base.Authorization;

namespace School.PartnerAdminApi.Partner.V1.EnableUser.Endpoint;

[Route("/v1/admin/school/partners/{pid:guid}/users/{uid}/enable")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1EnableUserEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid pid, string uid,
        [FromServices] OdinDbContext db,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] IPermissionService perms,
        HttpContext http,
        CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "partner.users.disable", ct) != AccessLevel.Edit) return Results.Forbid();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == uid && u.PartnerId == pid, ct);
        if (user is null) return Results.NotFound();
        user.IsEnabled = true;
        await userManager.UpdateAsync(user);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private const string Route = "/v1/admin/school/partners/{pid:guid}/users/{uid}/enable";
}
