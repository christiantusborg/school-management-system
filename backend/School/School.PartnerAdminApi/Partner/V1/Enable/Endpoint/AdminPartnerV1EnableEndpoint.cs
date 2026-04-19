namespace School.PartnerAdminApi.Partner.V1.Enable.Endpoint;

[Route("/v1/admin/school/partners/{id:guid}/enable")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1EnableEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] OdinDbContext db,
        [FromServices] UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        var partner = await db.Partners.FirstOrDefaultAsync(p => p.PartnerId == id, ct);
        if (partner is null) return Results.NotFound();

        partner.DeletedAt = null;

        var users = await db.Users
            .Where(u => u.PartnerId == id)
            .ToListAsync(ct);

        foreach (var u in users)
        {
            u.IsEnabled = true;
            await userManager.UpdateAsync(u);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private const string Route = "/v1/admin/school/partners/{id:guid}/enable";
}
