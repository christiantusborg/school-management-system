using Odin.Api.Base.Authorization;
using Odin.Api.Base.Programmes;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.ProgrammeApi;

/// <summary>
/// Specialization-level review of partner-owned programmes — approval moved
/// down from programme level. Approve / reject / reopen act on the
/// <see cref="PartnerSpecializationStatus"/> row; after every change the
/// parent programme's derived status is recomputed (live once ≥1 spec is
/// approved). Also hosts the admin-side clone: admission may clone a spec
/// from within the same programme or from any CORE programme of the same
/// award level, and admin clones are approved instantly.
/// </summary>
[Route("/v1/school/specializations/{id:guid}/approve")]
[EndpointTag("Admin.PartnerProgrammeReview")]
public sealed class AdminSpecializationsV1ReviewEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/school/specializations/{id:guid}/approve", ApproveAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/school/specializations/{id:guid}/reject", RejectAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/school/specializations/{id:guid}/reopen", ReopenAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/school/programmes/{programmeId:guid}/spec-clone-sources", CloneSourcesAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/school/programmes/{programmeId:guid}/specializations/clone", CloneAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class RejectRequest { public string? Reason { get; init; } }
    public sealed class CloneRequest { public Guid SourceSpecializationId { get; init; } }

    private static async Task<(Guid ProgrammeId, IResult? Fail)> ResolveCustomSpecAsync(
        OdinDbContext db, Guid specializationId, CancellationToken ct)
    {
        var spec = await db.Specializations
            .Where(s => s.SpecializationId == specializationId && s.DeletedAt == null)
            .Select(s => new { s.ProgrammeId, s.Programmes.OwnerId })
            .FirstOrDefaultAsync(ct);
        if (spec is null)
            return (Guid.Empty, Results.NotFound(new { error = "Specialization not found." }));
        if (spec.OwnerId is null)
            return (Guid.Empty, Results.BadRequest(new { error = "Core specializations have no approval workflow." }));
        return (spec.ProgrammeId, null);
    }

    private static async Task<IResult> ApproveAsync(
        OdinDbContext db, Guid id, HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "partner.programmes.approve", ct) != AccessLevel.Edit) return Results.Forbid();

        var (programmeId, fail) = await ResolveCustomSpecAsync(db, id, ct);
        if (fail is not null) return fail;

        var row = await SpecApproval.EnsureAsync(db, id, ct);
        row.Status = SpecApproval.StatusApproved;
        row.RejectionReason = null;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await SpecApproval.RecomputeProgrammeAsync(db, programmeId, ct);
        return Results.Ok(new { specializationId = id, status = "Approved" });
    }

    private static async Task<IResult> RejectAsync(
        OdinDbContext db, Guid id, [FromBody] RejectRequest body,
        HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "partner.programmes.reject", ct) != AccessLevel.Edit) return Results.Forbid();

        var reason = body?.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
            return Results.BadRequest(new { error = "Rejection reason is required." });

        var (programmeId, fail) = await ResolveCustomSpecAsync(db, id, ct);
        if (fail is not null) return fail;

        var row = await SpecApproval.EnsureAsync(db, id, ct);
        row.Status = SpecApproval.StatusRejected;
        row.RejectionReason = reason;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await SpecApproval.RecomputeProgrammeAsync(db, programmeId, ct);
        return Results.Ok(new { specializationId = id, status = "Rejected" });
    }

    private static async Task<IResult> ReopenAsync(
        OdinDbContext db, Guid id, HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "partner.programmes.reopen", ct) != AccessLevel.Edit) return Results.Forbid();

        var (programmeId, fail) = await ResolveCustomSpecAsync(db, id, ct);
        if (fail is not null) return fail;

        var row = await SpecApproval.EnsureAsync(db, id, ct);
        row.Status = SpecApproval.StatusPending;
        // Keep the RejectionReason as a paper trail until the next approve.
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await SpecApproval.RecomputeProgrammeAsync(db, programmeId, ct);
        return Results.Ok(new { specializationId = id, status = "Pending" });
    }

    private static async Task<IResult> CloneSourcesAsync(OdinDbContext db, Guid programmeId, CancellationToken ct)
    {
        var target = await db.Programmes
            .Where(p => p.ProgrammeId == programmeId && p.DeletedAt == null && p.OwnerId != null)
            .Select(p => new { p.AwardEducationLevelId })
            .FirstOrDefaultAsync(ct);
        if (target is null) return Results.NotFound(new { error = "Custom programme not found." });

        var items = await db.Specializations
            .Where(s => s.DeletedAt == null
                && (s.ProgrammeId == programmeId
                    || (s.Programmes.OwnerId == null && s.Programmes.DeletedAt == null
                        && s.Programmes.AwardEducationLevelId == target.AwardEducationLevelId)))
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
        OdinDbContext db, Guid programmeId, [FromBody] CloneRequest body, CancellationToken ct)
    {
        var isCustom = await db.Programmes
            .AnyAsync(p => p.ProgrammeId == programmeId && p.DeletedAt == null && p.OwnerId != null, ct);
        if (!isCustom) return Results.NotFound(new { error = "Custom programme not found." });

        if (!await SpecApproval.IsValidCloneSourceAsync(db, programmeId, body.SourceSpecializationId, ct))
            return Results.BadRequest(new { error = "Source must be a spec of this programme or of a core programme with the same award level." });

        var newSpecId = await SpecApproval.CloneSpecializationAsync(db, programmeId, body.SourceSpecializationId, ct);
        // Admission clones are trusted content: approved immediately.
        db.PartnerSpecializationStatuses.Add(new PartnerSpecializationStatus
        {
            SpecializationId = newSpecId,
            Status = SpecApproval.StatusApproved,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        await SpecApproval.RecomputeProgrammeAsync(db, programmeId, ct);
        return Results.Ok(new { specializationId = newSpecId, status = "Approved" });
    }
}
