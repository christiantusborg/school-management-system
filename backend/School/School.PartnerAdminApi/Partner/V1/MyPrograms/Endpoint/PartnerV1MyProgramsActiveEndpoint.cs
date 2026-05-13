using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs/{programmeId:guid}/active")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsActiveEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/partner/my-programs/{programmeId:guid}/active", HandleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class MyProgramsActiveRequest
    {
        public bool IsActive { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid programmeId, [FromBody] MyProgramsActiveRequest body,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
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
        if (status.Status != MyProgramsHelpers.StatusApproved)
            return Results.BadRequest(new { error = "Only approved programmes can be activated." });

        status.IsActive = body.IsActive;
        status.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId, isActive = status.IsActive });
    }
}
