using Odin.Api.Base.Documents;
using SharedLibrary.Basics.Opaque.Domains;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission-Office view + edit of an enrolment's per-module start dates.
/// Default (no override) is the enrolment's commencement date; overrides are
/// either an explicit date or commencement + N days (offset mode). ONLY the
/// Admission Office may write — partner and student have read-only endpoints.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/module-starts")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsModuleStartsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        const string route = "/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/module-starts";
        app.MapGet(route, GetAsync).RequireAuthorization("AdminOnly");
        app.MapPut(route, SaveAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class ModuleStartInput
    {
        public Guid SubjectId { get; init; }
        /// <summary>false = explicit StartDate; true = commencement + OffsetDays.</summary>
        public bool UseOffset { get; init; }
        public DateTime? StartDate { get; init; }
        public int? OffsetDays { get; init; }
        /// <summary>true resets the module back to the default (commencement).</summary>
        public bool ClearOverride { get; init; }
    }

    public sealed class SaveRequest
    {
        public List<ModuleStartInput>? Items { get; init; }
    }

    private static async Task<bool> OwnsAsync(OdinDbContext db, Guid studentId, Guid enrollmentId, CancellationToken ct) =>
        await db.Enrollments.AnyAsync(e => e.StudentEnrollmentId == enrollmentId
            && e.StudentId == studentId && e.DeletedAt == null, ct);

    private static async Task<IResult> GetAsync(
        Guid studentId, Guid enrollmentId, OdinDbContext db, CancellationToken ct)
    {
        if (!await OwnsAsync(db, studentId, enrollmentId, ct)) return Results.NotFound();
        var data = await ModuleStartService.ListAsync(db, enrollmentId, ct);
        return data is null ? Results.NotFound() : Results.Ok(data);
    }

    private static async Task<IResult> SaveAsync(
        Guid studentId, Guid enrollmentId, [FromBody] SaveRequest body,
        OdinDbContext db, CancellationToken ct)
    {
        if (!await OwnsAsync(db, studentId, enrollmentId, ct)) return Results.NotFound();

        var specId = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => e.SpecializationId)
            .FirstOrDefaultAsync(ct);
        var validSubjects = (await db.Subjects
            .Where(s => s.SpecializationId == specId && s.DeletedAt == null)
            .Select(s => s.SubjectId).ToListAsync(ct)).ToHashSet();

        var existing = await db.EnrollmentModuleStarts
            .Where(m => m.StudentEnrollmentId == enrollmentId)
            .ToListAsync(ct);
        var byId = existing.ToDictionary(m => m.SubjectId);

        foreach (var item in body.Items ?? [])
        {
            if (!validSubjects.Contains(item.SubjectId))
                return Results.BadRequest(new { error = "A module doesn't belong to this enrolment." });

            if (item.ClearOverride || (!item.UseOffset && item.StartDate is null) || (item.UseOffset && item.OffsetDays is null))
            {
                // Back to default: drop the override row if present.
                if (byId.TryGetValue(item.SubjectId, out var drop))
                    db.EnrollmentModuleStarts.Remove(drop);
                continue;
            }
            if (item.UseOffset && item.OffsetDays is < 0 or > 3650)
                return Results.BadRequest(new { error = "Offset days must be between 0 and 3650." });

            if (!byId.TryGetValue(item.SubjectId, out var row))
            {
                row = new EnrollmentModuleStart
                {
                    StudentEnrollmentId = enrollmentId,
                    SubjectId = item.SubjectId,
                };
                db.EnrollmentModuleStarts.Add(row);
            }
            row.UseOffset = item.UseOffset;
            row.StartDate = item.UseOffset ? null
                : item.StartDate is { } d ? DateTime.SpecifyKind(d.Date, DateTimeKind.Unspecified) : null;
            row.OffsetDays = item.UseOffset ? item.OffsetDays : null;
        }

        await db.SaveChangesAsync(ct);
        var data = await ModuleStartService.ListAsync(db, enrollmentId, ct);
        return Results.Ok(data);
    }
}
