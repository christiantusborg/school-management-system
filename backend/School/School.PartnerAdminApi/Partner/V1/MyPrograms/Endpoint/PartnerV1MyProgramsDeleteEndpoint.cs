namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs/{id:guid}")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(Route, EndpointHandlerAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] OdinDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var (_, partnerIdOrNull) = await MyProgramsHelpers.ResolvePartnerAsync(httpContext, db, ct);
        if (partnerIdOrNull is null) return Results.Forbid();
        var partnerId = partnerIdOrNull.Value;

        var programme = await db.Programmes
            .FirstOrDefaultAsync(p => p.ProgrammeId == id
                                   && p.PartnerId == partnerId
                                   && p.DeletedAt == null, ct);
        if (programme is null) return Results.NotFound();

        if (programme.IsDisabledByAdmin)
            return Results.Conflict(new { error = "disabled_by_admin" });

        if (await MyProgramsHelpers.HasEnrolmentsEverAsync(db, id, ct))
            return Results.Conflict(new { error = "has_enrolments" });

        var now = DateTime.UtcNow;
        programme.DeletedAt = now;

        var majors = await db.Majors
            .Where(m => m.ProgrammeId == id && m.DeletedAt == null)
            .Include(m => m.Subjects.Where(s => s.DeletedAt == null))
            .ToListAsync(ct);
        foreach (var m in majors)
        {
            m.DeletedAt = now;
            foreach (var s in m.Subjects) s.DeletedAt = now;
        }

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private const string Route = "/v1/partner/my-programs/{id:guid}";
}
