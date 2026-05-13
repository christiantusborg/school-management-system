namespace School.PartnerAdminApi.Partner.V1.Delete.Endpoint;

/// <summary>
/// Soft-deletes a partner by setting <c>DeletedAt</c>. Only allowed once the
/// partner is already disabled (admin must click Disable first) — guarding
/// against accidental deletion of an active partner. The row stays in the
/// database; admin can restore via the matching Restore endpoint.
/// </summary>
[Route("/v1/admin/school/partners/{id:guid}/delete")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1DeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/school/partners/{id:guid}/delete", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromServices] OdinDbContext db,
        CancellationToken ct)
    {
        var partner = await db.Partners.FirstOrDefaultAsync(p => p.PartnerId == id, ct);
        if (partner is null) return Results.NotFound();
        if (partner.DisabledAt is null)
            return Results.BadRequest("Partner must be disabled before it can be deleted.");
        if (partner.DeletedAt is not null) return Results.Ok(); // idempotent

        partner.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
