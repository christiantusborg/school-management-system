using Odin.Api.Base.Programmes;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

/// <summary>
/// Partner-side specialization workflow: per-spec submit for review, clone
/// sources listing and cloning. A partner may clone a specialization from
/// within the same custom programme or from any CORE programme sharing the
/// programme's award level; partner clones start as Draft and must be
/// submitted for admission approval.
/// </summary>
[Route("/v1/partner/my-programs/{programmeId:guid}/specializations")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MySpecializationsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-programs/{programmeId:guid}/specializations/{specializationId:guid}/submit", SubmitAsync)
            .RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my-programs/{programmeId:guid}/spec-clone-sources", CloneSourcesAsync)
            .RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my-programs/{programmeId:guid}/specializations/clone", CloneAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class CloneRequest { public Guid SourceSpecializationId { get; init; } }

    private static async Task<(bool Ok, IResult? Fail)> OwnsAsync(
        HttpContext httpContext, OdinDbContext db, Guid programmeId, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return (false, fail);
        var owns = await db.Programmes
            .AnyAsync(p => p.ProgrammeId == programmeId && p.OwnerId == partnerId && p.DeletedAt == null, ct);
        return owns ? (true, null) : (false, Results.NotFound());
    }

    private static async Task<IResult> SubmitAsync(
        Guid programmeId, Guid specializationId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (ok, fail) = await OwnsAsync(httpContext, db, programmeId, ct);
        if (!ok) return fail!;

        var belongs = await db.Specializations
            .AnyAsync(s => s.SpecializationId == specializationId && s.ProgrammeId == programmeId && s.DeletedAt == null, ct);
        if (!belongs) return Results.NotFound();

        var progStatus = await db.PartnerProgrammeStatuses
            .FirstOrDefaultAsync(s => s.ProgrammeId == programmeId, ct);
        if (progStatus?.IsDisabledByAdmin == true)
            return Results.BadRequest(new { error = "Programme is disabled by admin." });

        var row = await SpecApproval.EnsureAsync(db, specializationId, ct);
        if (row.Status is not (SpecApproval.StatusDraft or SpecApproval.StatusRejected))
            return Results.BadRequest(new { error = "Only Draft or Rejected specializations can be submitted." });

        row.Status = SpecApproval.StatusPending;
        row.RejectionReason = null;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await SpecApproval.RecomputeProgrammeAsync(db, programmeId, ct);
        return Results.Ok(new { specializationId, status = "Pending" });
    }

    private static async Task<IResult> CloneSourcesAsync(
        Guid programmeId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (ok, fail) = await OwnsAsync(httpContext, db, programmeId, ct);
        if (!ok) return fail!;

        var award = await db.Programmes
            .Where(p => p.ProgrammeId == programmeId)
            .Select(p => p.AwardEducationLevelId)
            .FirstOrDefaultAsync(ct);

        var items = await db.Specializations
            .Where(s => s.DeletedAt == null
                && (s.ProgrammeId == programmeId
                    || (s.Programmes.OwnerId == null && s.Programmes.DeletedAt == null
                        && s.Programmes.AwardEducationLevelId == award)))
            .OrderBy(s => s.Programmes.Code).ThenBy(s => s.Code)
            .Select(s => new
            {
                specializationId = s.SpecializationId,
                code = s.Code,
                name = s.Name,
                programmeCode = s.Programmes.Code,
                programmeName = s.Programmes.Name,
                source = s.ProgrammeId == programmeId ? "self" : "core",
            })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> CloneAsync(
        Guid programmeId, [FromBody] CloneRequest body, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (ok, fail) = await OwnsAsync(httpContext, db, programmeId, ct);
        if (!ok) return fail!;

        var progStatus = await db.PartnerProgrammeStatuses
            .FirstOrDefaultAsync(s => s.ProgrammeId == programmeId, ct);
        if (progStatus?.IsDisabledByAdmin == true)
            return Results.BadRequest(new { error = "Programme is disabled by admin." });

        if (!await SpecApproval.IsValidCloneSourceAsync(db, programmeId, body.SourceSpecializationId, ct))
            return Results.BadRequest(new { error = "Source must be a spec of this programme or of a core programme with the same award level." });

        var newSpecId = await SpecApproval.CloneSpecializationAsync(db, programmeId, body.SourceSpecializationId, ct);
        // Partner clones must pass admission review before going live.
        db.PartnerSpecializationStatuses.Add(new PartnerSpecializationStatus
        {
            SpecializationId = newSpecId,
            Status = SpecApproval.StatusDraft,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        await SpecApproval.RecomputeProgrammeAsync(db, programmeId, ct);
        return Results.Ok(new { specializationId = newSpecId, status = "Draft" });
    }
}
