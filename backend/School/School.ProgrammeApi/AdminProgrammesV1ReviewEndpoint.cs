using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.ProgrammeApi;

/// <summary>
/// Admin reviews of partner-owned programmes — approve / reject / reopen /
/// admin-disable toggle. Each modifies the
/// <see cref="PartnerProgrammeStatus"/> row that backs the programme's
/// workflow state. Core programmes (no status row) are out of scope: they're
/// implicitly approved and admin-controlled directly via the Academic page.
/// </summary>
[Route("/v1/school/programmes")]
[EndpointTag("Admin.PartnerProgrammeReview")]
public sealed class AdminProgrammesV1ReviewEndpoint : IEndpointMarker
{
    private const int StatusDraft     = 0;
    private const int StatusPending   = 1;
    private const int StatusApproved  = 2;
    private const int StatusRejected  = 3;

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/school/programmes/{id:guid}/approve",       ApproveAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/school/programmes/{id:guid}/reject",        RejectAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/school/programmes/{id:guid}/reopen",        ReopenAsync).RequireAuthorization("AdminOnly");
        app.MapMethods("/v1/school/programmes/{id:guid}/admin-disable", new[] { "PATCH" }, AdminDisableAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class RejectRequest
    {
        public string? Reason { get; init; }
    }

    public sealed class AdminDisableRequest
    {
        public bool Disabled { get; init; }
    }

    private static async Task<IResult> ApproveAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var status = await EnsureStatusAsync(db, id, ct);
        if (status is null) return Results.NotFound(new { error = "Programme not found or not partner-owned." });

        status.Status = StatusApproved;
        status.IsActive = !status.IsDisabledByAdmin;
        status.RejectionReason = null;
        status.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId = id, status = "Approved" });
    }

    private static async Task<IResult> RejectAsync(
        OdinDbContext db, Guid id, [FromBody] RejectRequest body, CancellationToken ct)
    {
        var reason = body?.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
            return Results.BadRequest(new { error = "Rejection reason is required." });

        var status = await EnsureStatusAsync(db, id, ct);
        if (status is null) return Results.NotFound(new { error = "Programme not found or not partner-owned." });

        status.Status = StatusRejected;
        status.IsActive = false;
        status.RejectionReason = reason;
        status.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId = id, status = "Rejected" });
    }

    private static async Task<IResult> ReopenAsync(OdinDbContext db, Guid id, CancellationToken ct)
    {
        var status = await EnsureStatusAsync(db, id, ct);
        if (status is null) return Results.NotFound(new { error = "Programme not found or not partner-owned." });

        status.Status = StatusPending;
        status.IsActive = false;
        // Keep the original RejectionReason as a paper trail; clearing it is
        // an admin choice on the next approve.
        status.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId = id, status = "Pending" });
    }

    private static async Task<IResult> AdminDisableAsync(
        OdinDbContext db, Guid id, [FromBody] AdminDisableRequest body, CancellationToken ct)
    {
        var status = await EnsureStatusAsync(db, id, ct);
        if (status is null) return Results.NotFound(new { error = "Programme not found or not partner-owned." });

        status.IsDisabledByAdmin = body.Disabled;
        // Disabling a programme implicitly deactivates it; enabling activates
        // again only if the programme has been approved.
        status.IsActive = !body.Disabled && status.Status == StatusApproved;
        status.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId = id, isDisabledByAdmin = status.IsDisabledByAdmin });
    }

    private static async Task<PartnerProgrammeStatus?> EnsureStatusAsync(OdinDbContext db, Guid programmeId, CancellationToken ct)
    {
        var programme = await db.Programmes
            .Where(p => p.ProgrammeId == programmeId && p.DeletedAt == null)
            .Select(p => new { p.OwnerId })
            .FirstOrDefaultAsync(ct);
        if (programme is null) return null;
        // Only partner-owned programmes have a workflow row.
        if (programme.OwnerId is null) return null;

        var status = await db.PartnerProgrammeStatuses.FirstOrDefaultAsync(s => s.ProgrammeId == programmeId, ct);
        if (status is null)
        {
            status = new PartnerProgrammeStatus
            {
                ProgrammeId = programmeId,
                Status = StatusDraft,
                IsActive = false,
                IsDisabledByAdmin = false,
                UpdatedAt = DateTime.UtcNow,
            };
            db.PartnerProgrammeStatuses.Add(status);
        }
        return status;
    }
}
