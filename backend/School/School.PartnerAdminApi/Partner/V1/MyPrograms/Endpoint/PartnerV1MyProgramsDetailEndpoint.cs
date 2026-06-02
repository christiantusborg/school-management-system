using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs/{programmeId:guid}")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsDetailEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-programs/{programmeId:guid}", HandleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid programmeId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var programme = await db.Programmes
            .Where(p => p.ProgrammeId == programmeId && p.OwnerId == partnerId && p.DeletedAt == null)
            .Select(p => new
            {
                p.ProgrammeId,
                p.Name,
                p.Code,
                p.MinDurationMonths,
                p.MaxDurationMonths,
            })
            .FirstOrDefaultAsync(ct);
        if (programme is null) return Results.NotFound();

        var status = await db.PartnerProgrammeStatuses
            .FirstOrDefaultAsync(s => s.ProgrammeId == programmeId, ct);

        var pathwayIds = await db.ProgrammePathways
            .Where(pp => pp.ProgrammeId == programmeId && pp.DeletedAt == null)
            .Select(pp => pp.PathwayId)
            .ToListAsync(ct);

        var hasEnrolments = await MyProgramsHelpers.HasEnrolmentsAsync(db, programmeId, ct);

        var specializations = await db.Specializations
            .Where(s => s.ProgrammeId == programmeId && s.DeletedAt == null)
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                specializationId = s.SpecializationId,
                name = s.Name,
                subjects = db.Subjects
                    .Where(sub => sub.SpecializationId == s.SpecializationId && sub.DeletedAt == null)
                    .OrderBy(sub => sub.Code)
                    .Select(sub => new
                    {
                        subjectId = sub.SubjectId,
                        code = sub.Code,
                        name = sub.Name,
                        ects = sub.Ects,
                    })
                    .ToList(),
            })
            .ToListAsync(ct);

        return Results.Ok(new
        {
            programmeId = programme.ProgrammeId,
            name = programme.Name,
            code = programme.Code,
            minDurationMonths = programme.MinDurationMonths,
            maxDurationMonths = programme.MaxDurationMonths,
            status = MyProgramsHelpers.StatusLabel(status?.Status ?? MyProgramsHelpers.StatusDraft),
            isActive = status?.IsActive ?? false,
            isDisabledByAdmin = status?.IsDisabledByAdmin ?? false,
            rejectionReason = status?.RejectionReason,
            hasEnrolments,
            canDelete = !hasEnrolments
                && (status?.Status ?? MyProgramsHelpers.StatusDraft) is MyProgramsHelpers.StatusDraft or MyProgramsHelpers.StatusRejected,
            pathwayIds,
            specializations,
        });
    }
}
