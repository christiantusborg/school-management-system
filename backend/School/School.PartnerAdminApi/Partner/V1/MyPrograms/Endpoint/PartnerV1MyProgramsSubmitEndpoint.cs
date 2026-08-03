using Odin.Api.Base.Programmes;
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

        // Approval is per specialization now: programme-level submit is a
        // convenience that sends every Draft/Rejected spec to review at once.
        var specIds = await db.Specializations
            .Where(s => s.ProgrammeId == programmeId && s.DeletedAt == null)
            .Select(s => s.SpecializationId)
            .ToListAsync(ct);
        if (specIds.Count == 0)
            return Results.BadRequest(new { error = "Add at least one specialization before submitting." });

        var submitted = 0;
        foreach (var specId in specIds)
        {
            var row = await SpecApproval.EnsureAsync(db, specId, ct);
            if (row.Status is not (SpecApproval.StatusDraft or SpecApproval.StatusRejected)) continue;
            row.Status = SpecApproval.StatusPending;
            row.RejectionReason = null;
            row.UpdatedAt = DateTime.UtcNow;
            submitted++;
        }
        if (submitted == 0)
            return Results.BadRequest(new { error = "No Draft or Rejected specializations to submit." });

        await db.SaveChangesAsync(ct);
        await SpecApproval.RecomputeProgrammeAsync(db, programmeId, ct);
        return Results.Ok(new { programmeId, submitted });
    }
}
