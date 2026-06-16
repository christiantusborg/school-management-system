using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs/{programmeId:guid}")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsUpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/partner/my-programs/{programmeId:guid}", HandleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class SubjectInput
    {
        public Guid? SubjectId { get; init; }
        public string? Code { get; init; }
        public string? Name { get; init; }
        public decimal Ects { get; init; }
    }

    public sealed class SpecializationInput
    {
        public Guid? SpecializationId { get; init; }
        public string? Name { get; init; }
        public List<SubjectInput>? Subjects { get; init; }
    }

    public sealed class UpdateRequest
    {
        public string? Name { get; init; }
        public string? Code { get; init; }
        public List<SpecializationInput>? Specializations { get; init; }
        public List<Guid>? PathwayIds { get; init; }
        public int? MinDurationMonths { get; init; }
        public int? MaxDurationMonths { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid programmeId, [FromBody] UpdateRequest body,
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var programme = await db.Programmes
            .FirstOrDefaultAsync(p => p.ProgrammeId == programmeId && p.OwnerId == partnerId && p.DeletedAt == null, ct);
        if (programme is null) return Results.NotFound();

        var status = await db.PartnerProgrammeStatuses
            .FirstOrDefaultAsync(s => s.ProgrammeId == programmeId, ct);
        if (status is null) return Results.NotFound();

        if (status.IsDisabledByAdmin)
            return Results.BadRequest(new { error = "Programme is disabled by admin and cannot be edited." });
        if (await MyProgramsHelpers.HasEnrolmentsAsync(db, programmeId, ct))
            return Results.BadRequest(new { error = "Programme has enrolled students and cannot be edited." });
        if (status.Status is MyProgramsHelpers.StatusPending)
            return Results.BadRequest(new { error = "Programme is pending approval and cannot be edited." });

        if (!string.IsNullOrWhiteSpace(body.Name)) programme.Name = body.Name.Trim();
        if (!string.IsNullOrWhiteSpace(body.Code)) programme.Code = body.Code.Trim();

        var newMin = body.MinDurationMonths ?? programme.MinDurationMonths;
        var newMax = body.MaxDurationMonths ?? programme.MaxDurationMonths;
        if (newMin < 1 || newMax < newMin)
            return Results.BadRequest(new { error = "Invalid duration range: need 1 ≤ min ≤ max." });
        programme.MinDurationMonths = newMin;
        programme.MaxDurationMonths = newMax;

        // Sync specializations + subjects (server is the truth: rows not in the
        // payload get soft-deleted, missing IDs are treated as new).
        if (body.Specializations is not null)
        {
            var existingSpecs = await db.Specializations
                .Where(s => s.ProgrammeId == programmeId && s.DeletedAt == null)
                .ToListAsync(ct);
            var keepSpecIds = body.Specializations
                .Where(s => s.SpecializationId is not null)
                .Select(s => s.SpecializationId!.Value)
                .ToHashSet();
            foreach (var existing in existingSpecs.Where(e => !keepSpecIds.Contains(e.SpecializationId)))
                existing.DeletedAt = DateTime.UtcNow;

            foreach (var input in body.Specializations)
            {
                var name = (input.Name ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name)) continue;

                Specialization spec;
                if (input.SpecializationId is { } sid && existingSpecs.FirstOrDefault(e => e.SpecializationId == sid) is { } existing)
                {
                    spec = existing;
                    spec.Name = name;
                }
                else
                {
                    spec = new Specialization
                    {
                        SpecializationId = Guid.NewGuid(),
                        ProgrammeId = programmeId,
                        Name = name,
                        Code = $"S-{Guid.NewGuid().ToString()[..8]}",
                        Description = string.Empty,
                    };
                    db.Specializations.Add(spec);
                }

                if (input.Subjects is null) continue;
                var existingSubs = await db.Subjects
                    .Where(s => s.SpecializationId == spec.SpecializationId && s.DeletedAt == null)
                    .ToListAsync(ct);
                var keepSubIds = input.Subjects
                    .Where(s => s.SubjectId is not null)
                    .Select(s => s.SubjectId!.Value)
                    .ToHashSet();
                foreach (var dropped in existingSubs.Where(s => !keepSubIds.Contains(s.SubjectId)))
                    dropped.DeletedAt = DateTime.UtcNow;

                foreach (var subInput in input.Subjects)
                {
                    var subName = (subInput.Name ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(subName)) continue;
                    if (subInput.SubjectId is { } subId && existingSubs.FirstOrDefault(s => s.SubjectId == subId) is { } existingSub)
                    {
                        existingSub.Code = (subInput.Code ?? string.Empty).Trim();
                        existingSub.Name = subName;
                        existingSub.Ects = subInput.Ects;
                    }
                    else
                    {
                        db.Subjects.Add(new Subject
                        {
                            SubjectId = Guid.NewGuid(),
                            SpecializationId = spec.SpecializationId,
                            Code = (subInput.Code ?? string.Empty).Trim(),
                            Name = subName,
                            Description = string.Empty,
                            Ects = subInput.Ects,
                        });
                    }
                }
            }
        }

        // Sync pathways
        if (body.PathwayIds is not null)
        {
            var requested = body.PathwayIds.Distinct().ToHashSet();
            var existing = await db.ProgrammePathways
                .Where(pp => pp.ProgrammeId == programmeId && pp.DeletedAt == null)
                .ToListAsync(ct);
            foreach (var pp in existing.Where(p => !requested.Contains(p.PathwayId)))
                pp.DeletedAt = DateTime.UtcNow;
            var existingIds = existing.Where(p => p.DeletedAt == null).Select(p => p.PathwayId).ToHashSet();
            foreach (var pid in requested.Where(p => !existingIds.Contains(p)))
            {
                db.ProgrammePathways.Add(new ProgrammePathway
                {
                    ProgrammePathwayId = Guid.NewGuid(),
                    ProgrammeId = programmeId,
                    PathwayId = pid,
                });
            }
        }

        // Editing an Approved programme flips it back to Pending review.
        if (status.Status == MyProgramsHelpers.StatusApproved)
        {
            status.Status = MyProgramsHelpers.StatusPending;
            status.IsActive = false;
        }
        status.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { programmeId });
    }
}
