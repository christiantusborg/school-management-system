namespace School.PartnerAdminApi.Partner.V1.RevokeProgrammeAccess.Endpoint;

[Route("/v1/admin/school/partners/{id:guid}/programme-access/{majorId:guid}")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1RevokeProgrammeAccessEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        Guid majorId,
        [FromServices] OdinDbContext db,
        CancellationToken ct)
    {
        var row = await db.PartnerProgrammeAccesses
            .FirstOrDefaultAsync(a => a.PartnerId == id && a.MajorId == majorId && a.DeletedAt == null, ct);
        if (row is null) return Results.NotFound();

        row.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private const string Route = "/v1/admin/school/partners/{id:guid}/programme-access/{majorId:guid}";
}
