namespace School.PartnerAdminApi.Partner.V1.DisableUser.Endpoint;

[Route("/v1/admin/school/partners/{pid:guid}/users/{uid}/disable")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1DisableUserEndpoint : IEndpointMarker
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
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == uid && u.PartnerId == pid, ct);
        if (user is null) return Results.NotFound();
        user.IsEnabled = false;
        await userManager.UpdateAsync(user);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private const string Route = "/v1/admin/school/partners/{pid:guid}/users/{uid}/disable";
}
