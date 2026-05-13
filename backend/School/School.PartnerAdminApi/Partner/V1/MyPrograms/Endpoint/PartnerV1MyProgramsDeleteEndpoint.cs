using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs/{programmeId:guid}")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/v1/partner/my-programs/{programmeId:guid}", HandleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid programmeId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var programme = await db.Programmes
            .FirstOrDefaultAsync(p => p.ProgrammeId == programmeId && p.OwnerId == partnerId && p.DeletedAt == null, ct);
        if (programme is null) return Results.NotFound();

        var status = await db.PartnerProgrammeStatuses.FirstOrDefaultAsync(s => s.ProgrammeId == programmeId, ct);
        if (await MyProgramsHelpers.HasEnrolmentsAsync(db, programmeId, ct))
            return Results.BadRequest(new { error = "Programme has enrolled students and cannot be deleted." });
        if (status is not null && status.Status is not (MyProgramsHelpers.StatusDraft or MyProgramsHelpers.StatusRejected))
            return Results.BadRequest(new { error = "Only Draft or Rejected programmes can be deleted." });

        programme.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId, deleted = true });
    }
}
