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
        if (partner.DeletedAt is not null) return Results.BadRequest("Partner is deleted — restore before enabling.");

        // Re-enable. DeletedAt is independent — it stays as is (null for a
        // non-deleted partner, which is the only state we accept here).
        partner.DisabledAt = null;

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
