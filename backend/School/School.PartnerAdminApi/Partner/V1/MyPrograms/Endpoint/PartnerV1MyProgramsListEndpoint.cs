using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-programs", HandleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var programmes = await db.Programmes
            .Where(p => p.OwnerId == partnerId && p.DeletedAt == null)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.ProgrammeId,
                p.Name,
                p.Code,
                Status = db.PartnerProgrammeStatuses
                    .Where(s => s.ProgrammeId == p.ProgrammeId)
                    .Select(s => (PartnerProgrammeStatus?)s)
                    .FirstOrDefault(),
                PathwayIds = db.ProgrammePathways
                    .Where(pp => pp.ProgrammeId == p.ProgrammeId && pp.DeletedAt == null)
                    .Select(pp => pp.PathwayId)
                    .ToList(),
                HasEnrolments = db.Enrollments
                    .Any(e => e.DeletedAt == null && e.Specialization.ProgrammeId == p.ProgrammeId),
            })
            .ToListAsync(ct);

        var items = programmes.Select(p => new
        {
            programmeId = p.ProgrammeId,
            name = p.Name,
            code = p.Code,
            status = MyProgramsHelpers.StatusLabel(p.Status?.Status ?? MyProgramsHelpers.StatusDraft),
            isActive = p.Status?.IsActive ?? false,
            isDisabledByAdmin = p.Status?.IsDisabledByAdmin ?? false,
            rejectionReason = p.Status?.RejectionReason,
            hasEnrolments = p.HasEnrolments,
            canDelete = !p.HasEnrolments
                && (p.Status?.Status ?? MyProgramsHelpers.StatusDraft) is MyProgramsHelpers.StatusDraft or MyProgramsHelpers.StatusRejected,
            pathwayIds = p.PathwayIds,
        }).ToList();

        return Results.Ok(new { items, total = items.Count });
    }
}
