namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs/{id:guid}")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsUpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch(Route, EndpointHandlerAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromBody] PartnerV1MyProgramsUpdateRequest request,
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
            return Results.Conflict(new { error = "locked_has_enrolments" });

        if (programme.Status == ProgrammeStatus.Pending)
            return Results.Conflict(new { error = "locked_awaiting_approval" });

        var newName = (request.Name ?? string.Empty).Trim();
        var newCode = (request.Code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(newName)) return Results.BadRequest(new { error = "name_required" });
        if (string.IsNullOrWhiteSpace(newCode)) return Results.BadRequest(new { error = "code_required" });

        if (newCode != programme.Code)
        {
            var codeTaken = await db.Programmes.AnyAsync(
                p => p.Code == newCode && p.ProgrammeId != id, ct);
            if (codeTaken) return Results.Conflict(new { error = "code_not_unique" });
        }

        var requestedPathwayIds = request.PathwayIds is null
            ? null
            : request.PathwayIds.Where(pid => pid > 0).Distinct().ToList();
        if (requestedPathwayIds is { Count: > 0 } &&
            !await MyProgramsHelpers.ValidatePathwayIdsAsync(db, requestedPathwayIds, ct))
        {
            return Results.BadRequest(new { error = "invalid_pathway_ids" });
        }

        programme.Name = newName;
        programme.Code = newCode;

        if (programme.Status == ProgrammeStatus.Approved)
        {
            programme.Status = ProgrammeStatus.Pending;
            programme.ApprovedAt = null;
            programme.SubmittedAt = DateTime.UtcNow;
        }
        else if (programme.Status == ProgrammeStatus.Rejected)
        {
            programme.RejectionReason = null;
        }

        var existingMajors = await db.Majors
            .Where(m => m.ProgrammeId == id && m.DeletedAt == null)
            .Include(m => m.Subjects.Where(s => s.DeletedAt == null))
            .ToListAsync(ct);

        var incomingMajorIds = request.Majors
            .Where(m => m.MajorId is not null)
            .Select(m => m.MajorId!.Value)
            .ToHashSet();

        var now = DateTime.UtcNow;
        foreach (var existing in existingMajors.Where(m => !incomingMajorIds.Contains(m.MajorId)))
        {
            existing.DeletedAt = now;
            foreach (var s in existing.Subjects) s.DeletedAt = now;
        }

        foreach (var incomingMajor in request.Majors)
        {
            var name = (incomingMajor.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            Major major;
            if (incomingMajor.MajorId is Guid existingMajorId
                && existingMajors.FirstOrDefault(m => m.MajorId == existingMajorId) is { } match)
            {
                match.Name = name;
                major = match;
            }
            else
            {
                major = new Major
                {
                    MajorId = Guid.NewGuid(),
                    ProgrammeId = id,
                    Name = name,
                };
                db.Majors.Add(major);
            }

            var incomingSubjectIds = incomingMajor.Subjects
                .Where(s => s.SubjectId is not null)
                .Select(s => s.SubjectId!.Value)
                .ToHashSet();

            foreach (var s in major.Subjects.Where(s => !incomingSubjectIds.Contains(s.SubjectId)))
            {
                s.DeletedAt = now;
            }

            foreach (var incomingSubject in incomingMajor.Subjects)
            {
                var subjectCode = (incomingSubject.Code ?? string.Empty).Trim();
                var subjectName = (incomingSubject.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(subjectCode) && string.IsNullOrWhiteSpace(subjectName)) continue;

                if (incomingSubject.SubjectId is Guid existingSubjectId
                    && major.Subjects.FirstOrDefault(s => s.SubjectId == existingSubjectId) is { } subjectMatch)
                {
                    subjectMatch.Code = subjectCode;
                    subjectMatch.Name = subjectName;
                    subjectMatch.Ects = incomingSubject.Ects;
                }
                else
                {
                    db.Subjects.Add(new Subject
                    {
                        SubjectId = Guid.NewGuid(),
                        MajorId = major.MajorId,
                        Code = subjectCode,
                        Name = subjectName,
                        Ects = incomingSubject.Ects,
                    });
                }
            }
        }

        if (requestedPathwayIds is not null)
        {
            await MyProgramsHelpers.SyncProgrammePathwaysAsync(db, id, requestedPathwayIds, ct);
        }

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private const string Route = "/v1/partner/my-programs/{id:guid}";
}

public sealed class PartnerV1MyProgramsUpdateRequest
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required IReadOnlyList<PartnerV1MyProgramsUpdateMajor> Majors { get; init; }
    public IReadOnlyList<int>? PathwayIds { get; init; }
}

public sealed class PartnerV1MyProgramsUpdateMajor
{
    public Guid? MajorId { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<PartnerV1MyProgramsUpdateSubject> Subjects { get; init; }
}

public sealed class PartnerV1MyProgramsUpdateSubject
{
    public Guid? SubjectId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int Ects { get; init; }
}
