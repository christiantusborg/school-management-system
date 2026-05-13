namespace School.PartnerAdminApi.Partner.V1.Disable.Endpoint;

[Route("/v1/admin/school/partners/{id:guid}/disable")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1DisableEndpoint : IEndpointMarker
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
        if (partner.DeletedAt is not null) return Results.BadRequest("Partner is deleted — restore before disabling.");

        // Disable (not delete). The Delete button is what the admin clicks
        // afterwards to soft-delete. Users get locked out either way.
        partner.DisabledAt = DateTime.UtcNow;

        var users = await db.Users
            .Where(u => u.PartnerId == id)
            .ToListAsync(ct);

        foreach (var u in users)
        {
            u.IsEnabled = false;
            await userManager.UpdateAsync(u);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private const string Route = "/v1/admin/school/partners/{id:guid}/disable";
}
