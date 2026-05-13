using Microsoft.AspNetCore.Identity;

namespace School.PartnerAdminApi.Partner.V1.MyUsers.Endpoint;

[Route("/v1/partner/my-users/{uid}")]
[EndpointTag("Partner.MyUsers")]
public sealed class PartnerV1MyUsersDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(Route, EndpointHandlerAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        string uid,
        HttpContext httpContext,
        [FromServices] OdinDbContext db,
        [FromServices] UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        var (callerId, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        if (callerId == uid)
            return Results.Conflict(new { error = "cannot_delete_self" });

        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.Id == uid && u.PartnerId == partnerId && u.DeletedAt == null, ct);
        if (user is null) return Results.NotFound();

        user.DeletedAt = DateTime.UtcNow;
        user.IsEnabled = false;
        await userManager.UpdateAsync(user);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private const string Route = "/v1/partner/my-users/{uid}";
}
