namespace School.PartnerAdminApi.Partner.V1.Restore.Endpoint;

/// <summary>
/// Reverses a soft-delete by clearing <c>DeletedAt</c>. The partner stays
/// disabled (DisabledAt is preserved) so the admin can review it before
/// re-enabling. Idempotent — calling on an already-restored partner is a
/// no-op.
/// </summary>
[Route("/v1/admin/school/partners/{id:guid}/restore")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1RestoreEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/school/partners/{id:guid}/restore", HandleAsync)
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
        if (partner.DeletedAt is null) return Results.Ok(); // idempotent

        partner.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
