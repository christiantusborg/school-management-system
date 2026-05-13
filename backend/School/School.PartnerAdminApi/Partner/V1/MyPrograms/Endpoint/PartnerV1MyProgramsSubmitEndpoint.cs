using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs/{programmeId:guid}/submit")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsSubmitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-programs/{programmeId:guid}/submit", HandleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid programmeId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var owns = await db.Programmes
            .AnyAsync(p => p.ProgrammeId == programmeId && p.OwnerId == partnerId && p.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();

        var status = await db.PartnerProgrammeStatuses.FirstOrDefaultAsync(s => s.ProgrammeId == programmeId, ct);
        if (status is null) return Results.NotFound();

        if (status.IsDisabledByAdmin)
            return Results.BadRequest(new { error = "Programme is disabled by admin." });
        if (status.Status is not (MyProgramsHelpers.StatusDraft or MyProgramsHelpers.StatusRejected))
            return Results.BadRequest(new { error = "Programme can only be submitted from Draft or Rejected." });

        status.Status = MyProgramsHelpers.StatusPending;
        status.RejectionReason = null;
        status.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId, status = MyProgramsHelpers.StatusLabel(status.Status) });
    }
}
