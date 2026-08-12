using System.Security.Claims;
using Odin.Api.Base.Authorization;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission Office commits per-subject grades on behalf of the partner.
/// Same write logic as <c>PartnerV1MyStudentsSubmitGradesEndpoint</c>:
/// upserts <see cref="SubjectGrade"/> rows and flips the enrolment to
/// <c>AwaitingGradesApproval</c>. The partner-ownership filter is dropped
/// and the audit note records the admin as the actor. Admin can still
/// separately call approve-grades afterwards to finalise.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/grades")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsSubmitGradesEndpoint : IEndpointMarker
{
    private static readonly HashSet<Guid> AllowedFromStatuses = new()
    {
        EnrollmentStatusIds.AcceptOffer,
        EnrollmentStatusIds.ApplicationApprovedAdmission,
        EnrollmentStatusIds.AcceptAdmission,
        EnrollmentStatusIds.AwaitingGradesSubmit,
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/grades", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class GradeEntry
    {
        public Guid SubjectId { get; init; }
        public int? Score { get; init; }
        /// <summary>Rubric-graded modules: one 1-100 score per rubric row;
        /// the module grade is always the weighted total, never Score.</summary>
        public List<School.PartnerAdminApi.Admin.V1.Rubrics.RubricGradeLogic.RubricEntryDto>? Rubric { get; init; }
    }

    public sealed class SubmitGradesRequest
    {
        public List<GradeEntry>? Items { get; init; }
        public string? ProjectTitle { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, Guid enrollmentId, [FromBody] SubmitGradesRequest body,
        HttpContext httpContext, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "student.grades.submit", ct) != AccessLevel.Edit) return Results.Forbid();

        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var enrolment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentEnrollmentId == enrollmentId
                && e.StudentId == studentId
                && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();

        if (!AllowedFromStatuses.Contains(enrolment.StatusId))
            return Results.BadRequest(new { error = "This enrolment isn't ready for grade submission yet." });

        var entries = body.Items ?? new();
        if (entries.Count == 0)
            return Results.BadRequest(new { error = "No grades supplied." });

        var specSubjects = await db.Subjects
            .Where(s => s.SpecializationId == enrolment.SpecializationId && s.DeletedAt == null)
            .Select(s => new { s.SubjectId, s.Ects })
            .ToListAsync(ct);
        var validSet = specSubjects.Select(s => s.SubjectId).ToHashSet();
        var ectsBySubject = specSubjects.ToDictionary(s => s.SubjectId, s => s.Ects);

        // Rubric-graded modules: the final mark is ALWAYS the weighted total
        // of the row scores; simple modules keep the direct 0-100 mark.
        var rubricRowsBySubject = await School.PartnerAdminApi.Admin.V1.Rubrics.RubricGradeLogic.LoadRowsAsync(
            db, entries.Select(x => x.SubjectId), ct);
        var finalScores = new Dictionary<Guid, int>();
        var rubricScores = new Dictionary<Guid, List<(Guid RowId, int Score)>>();
        foreach (var entry in entries)
        {
            if (!validSet.Contains(entry.SubjectId))
                return Results.BadRequest(new { error = $"Subject {entry.SubjectId} doesn't belong to this enrolment." });
            if (rubricRowsBySubject.TryGetValue(entry.SubjectId, out var rubricRows) && rubricRows.Count > 0)
            {
                var (final, error) = School.PartnerAdminApi.Admin.V1.Rubrics.RubricGradeLogic.Compute(rubricRows, entry.Rubric);
                if (error is not null) return Results.BadRequest(new { error });
                finalScores[entry.SubjectId] = final;
                rubricScores[entry.SubjectId] = rubricRows
                    .Select(r => (r.RowId, entry.Rubric!.First(rs => rs.RowId == r.RowId).Score!.Value)).ToList();
            }
            else
            {
                if (entry.Score is null)
                    return Results.BadRequest(new { error = "Score is required." });
                if (entry.Score is < 0 or > 100)
                    return Results.BadRequest(new { error = "Score must be between 0 and 100." });
                finalScores[entry.SubjectId] = entry.Score.Value;
            }
        }

        var existing = await db.Set<SubjectGrade>()
            .Where(g => g.StudentEnrollmentId == enrollmentId)
            .ToListAsync(ct);
        var byId = existing.ToDictionary(g => g.SubjectId);

        // Completion gate: ECTS of every completed subject (already-saved grades
        // ∪ this submission) must reach the programme's required-ECTS threshold.
        // Null threshold = no gate.
        var requiredEcts = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => e.Specialization.Programmes.RequiredEcts)
            .FirstOrDefaultAsync(ct);
        if (requiredEcts is { } required && required > 0)
        {
            var completedSubjectIds = existing.Select(g => g.SubjectId)
                .Concat(entries.Select(e => e.SubjectId)).ToHashSet();
            var completedEcts = completedSubjectIds
                .Sum(id => ectsBySubject.TryGetValue(id, out var ec) ? ec : 0m);
            if (completedEcts < required)
                return Results.BadRequest(new
                {
                    error = $"Completion threshold not met: {completedEcts:0.#} of {required:0.#} required ECTS. "
                        + "Enter grades for more subjects before submitting."
                });
        }

        var now = DateTime.UtcNow;
        var pendingRubric = new List<(Guid GradeId, List<(Guid RowId, int Score)> Scores)>();
        foreach (var entry in entries)
        {
            if (!byId.TryGetValue(entry.SubjectId, out var row))
            {
                row = new SubjectGrade
                {
                    SubjectGradeId = Guid.NewGuid(),
                    StudentEnrollmentId = enrollmentId,
                    SubjectId = entry.SubjectId,
                };
                db.Set<SubjectGrade>().Add(row);
                byId[entry.SubjectId] = row;
            }
            row.Score = finalScores[entry.SubjectId];
            row.GradedAt = now;
            if (rubricScores.TryGetValue(entry.SubjectId, out var perRow))
                pendingRubric.Add((row.SubjectGradeId, perRow));
        }
        await School.PartnerAdminApi.Admin.V1.Rubrics.RubricGradeLogic.UpsertScoresAsync(db, pendingRubric, now, ct);

        enrolment.ProjectTitle = string.IsNullOrWhiteSpace(body.ProjectTitle) ? null : body.ProjectTitle.Trim();
        enrolment.StatusId = EnrollmentStatusIds.AwaitingGradesApproval;
        db.Set<EnrollmentStatusNote>().Add(new EnrollmentStatusNote
        {
            EnrollmentStatusNoteId = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            StatusId = EnrollmentStatusIds.AwaitingGradesApproval,
            Note = $"Admission Office submitted grades for {entries.Count} subject(s).",
            ByUserId = ParseUserGuid(callerId),
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            enrollmentId,
            statusCode = "AwaitingGradesApproval",
            count = entries.Count,
        });
    }

    private static Guid ParseUserGuid(string? userId) =>
        Guid.TryParse(userId, out var g) ? g : Guid.Empty;
}
