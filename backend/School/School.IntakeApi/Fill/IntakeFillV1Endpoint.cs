using System.Text.Json;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace School.IntakeApi.Fill;

/// <summary>
/// Fill-out surfaces for intake forms: the student portal
/// (/v1/student/me/intake-forms, Student role via path guard) and the partner
/// portal (/v1/partner/intake-forms, Partner role). Each respondent has at
/// most one response per instance; a draft can be saved repeatedly and
/// submitting freezes it with the template's version hash. The signup-wizard
/// survey has its own endpoint in PublicSignupApi (wizard-token auth).
/// </summary>
[Route("/v1/student/me/intake-forms")]
[EndpointTag("Intake.Fill")]
public sealed class IntakeFillV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/student/me/intake-forms", StudentListAsync).RequireAuthorization();
        app.MapPut("/v1/student/me/intake-forms/{intakeInstanceId:guid}/response", StudentSaveAsync).RequireAuthorization();
        app.MapPost("/v1/student/me/intake-forms/{intakeInstanceId:guid}/response/submit", StudentSubmitAsync).RequireAuthorization();

        app.MapGet("/v1/partner/intake-forms", PartnerListAsync).RequireAuthorization();
        app.MapPut("/v1/partner/intake-forms/{intakeInstanceId:guid}/response", PartnerSaveAsync).RequireAuthorization();
        app.MapPost("/v1/partner/intake-forms/{intakeInstanceId:guid}/response/submit", PartnerSubmitAsync).RequireAuthorization();
        return app;
    }

    public sealed class SaveBody
    {
        public string? AnswersJson { get; init; }
    }

    private static IResult Ok(object data) => Results.Ok(new { success = true, data });
    private static IResult Fail(string error, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { success = false, error }, statusCode: status);

    private static bool IsValidJson(string s)
    {
        try { using var _ = JsonDocument.Parse(s); return true; }
        catch { return false; }
    }

    // ── Respondent resolution ─────────────────────────────────────────────

    private static async Task<Guid?> StudentIdAsync(HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return null;
        var id = await db.Students.Where(s => s.UserId == userId).Select(s => (Guid?)s.StudentId).FirstOrDefaultAsync(ct);
        return id;
    }

    private static async Task<Guid?> PartnerIdAsync(HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return null;
        return await db.Users.Where(u => u.Id == userId).Select(u => u.PartnerId).FirstOrDefaultAsync(ct);
    }

    // ── Shared handlers ───────────────────────────────────────────────────

    /// <summary>
    /// Does a Targeted instance apply to this respondent? Programme /
    /// specialization / module targets resolve against the student's CURRENT
    /// enrolments, so a student enrolled after the assignment was created is
    /// picked up automatically.
    /// </summary>
    public static async Task<HashSet<Guid>> VisibleTargetedInstanceIdsAsync(
        OdinDbContext db, IReadOnlyCollection<Guid> targetedInstanceIds,
        Guid? studentId, Guid? partnerId, CancellationToken ct)
    {
        if (targetedInstanceIds.Count == 0) return [];
        var rows = await db.IntakeAssignments
            .Where(a => targetedInstanceIds.Contains(a.IntakeInstanceId))
            .ToListAsync(ct);
        if (rows.Count == 0) return [];

        HashSet<Guid> progIds = [], specIds = [];
        Guid? studentsPartnerId = null;
        if (studentId is { } sid)
        {
            var enr = await db.Enrollments
                .Where(e => e.StudentId == sid && e.DeletedAt == null)
                .Select(e => new { e.SpecializationId, e.Specialization.ProgrammeId, e.PartnerId })
                .ToListAsync(ct);
            specIds = enr.Select(e => e.SpecializationId).ToHashSet();
            progIds = enr.Select(e => e.ProgrammeId).ToHashSet();
            studentsPartnerId = enr.Select(e => (Guid?)e.PartnerId).FirstOrDefault()
                ?? await db.Students.Where(s => s.StudentId == sid).Select(s => (Guid?)s.PartnerId).FirstOrDefaultAsync(ct);
        }
        // Module targets match when the module's specialization is one the
        // student is enrolled in.
        var subjectIds = rows.Where(r => r.SubjectId != null).Select(r => r.SubjectId!.Value).Distinct().ToList();
        var subjectSpec = subjectIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await db.Subjects.Where(s => subjectIds.Contains(s.SubjectId))
                .ToDictionaryAsync(s => s.SubjectId, s => s.SpecializationId, ct);

        var visible = new HashSet<Guid>();
        foreach (var a in rows)
        {
            var match =
                (a.StudentId is { } ts && ts == studentId)
                || (a.PartnerId is { } tp && (tp == partnerId || tp == studentsPartnerId))
                || (a.ProgrammeId is { } tpr && progIds.Contains(tpr))
                || (a.SpecializationId is { } tsp && specIds.Contains(tsp))
                || (a.SubjectId is { } tsu && subjectSpec.TryGetValue(tsu, out var subSpec) && specIds.Contains(subSpec));
            if (match) visible.Add(a.IntakeInstanceId);
        }
        return visible;
    }

    private static async Task<IResult> ListForAsync(
        string audience, Guid? studentId, Guid? partnerId, OdinDbContext db, CancellationToken ct)
    {
        var items = await db.IntakeInstances
            .Where(i => i.DeletedAt == null && i.IsActive && i.Audience == audience
                && i.QuestionnaireTemplate != null && i.QuestionnaireTemplate.DeletedAt == null)
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                intakeInstanceId = i.IntakeInstanceId,
                name = i.Name,
                assignmentMode = i.AssignmentMode,
                definitionJson = i.QuestionnaireTemplate!.DefinitionJson,
                response = db.IntakeResponses
                    .Where(r => r.IntakeInstanceId == i.IntakeInstanceId && r.DeletedAt == null
                        && (studentId != null ? r.StudentId == studentId : r.PartnerId == partnerId))
                    .Select(r => new
                    {
                        lifecycleState = r.LifecycleState.ToString(),
                        answersJson = r.AnswersJson,
                        submittedAt = r.SubmittedAt,
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        // Targeted instances: keep only those whose assignments match this
        // respondent (specific user, partner, programme, spec or module).
        var targetedIds = items
            .Where(i => i.assignmentMode == IntakeInstance.ModeTargeted)
            .Select(i => i.intakeInstanceId).ToList();
        var visibleTargeted = await VisibleTargetedInstanceIdsAsync(db, targetedIds, studentId, partnerId, ct);
        var filtered = items
            .Where(i => i.assignmentMode != IntakeInstance.ModeTargeted || visibleTargeted.Contains(i.intakeInstanceId))
            .Select(i => new { i.intakeInstanceId, i.name, i.definitionJson, i.response });

        return Ok(new { items = filtered });
    }

    private static async Task<IResult> SaveForAsync(
        Guid intakeInstanceId, string audience, Guid? studentId, Guid? partnerId,
        string? answersJson, bool submit, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var instance = await db.IntakeInstances
            .Include(i => i.QuestionnaireTemplate)
            .FirstOrDefaultAsync(i => i.IntakeInstanceId == intakeInstanceId
                && i.DeletedAt == null && i.IsActive && i.Audience == audience, ct);
        if (instance is null) return Fail("not_found", StatusCodes.Status404NotFound);

        if (answersJson is not null && !IsValidJson(answersJson))
            return Fail("answers_not_valid_json");

        var response = await db.IntakeResponses.FirstOrDefaultAsync(r =>
            r.IntakeInstanceId == intakeInstanceId && r.DeletedAt == null
            && (studentId != null ? r.StudentId == studentId : r.PartnerId == partnerId), ct);

        if (response is not null && response.LifecycleState == IntakeResponseLifecycleState.Submitted)
            return Fail("already_submitted", StatusCodes.Status409Conflict);

        if (response is null)
        {
            response = new IntakeResponse
            {
                IntakeResponseId = Guid.NewGuid(),
                IntakeInstanceId = intakeInstanceId,
                StudentId = studentId,
                PartnerId = partnerId,
                CreatedByUserId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            };
            db.IntakeResponses.Add(response);
        }

        if (answersJson is not null) response.AnswersJson = answersJson;
        response.ModifiedAt = DateTime.UtcNow;
        if (submit)
        {
            response.LifecycleState = IntakeResponseLifecycleState.Submitted;
            response.SubmittedAt = DateTime.UtcNow;
            response.QuestionnaireVersionHash = instance.QuestionnaireTemplate?.DefinitionHash;
        }
        await db.SaveChangesAsync(ct);
        return Ok(new
        {
            intakeResponseId = response.IntakeResponseId,
            lifecycleState = response.LifecycleState.ToString(),
            submittedAt = response.SubmittedAt,
        });
    }

    // ── Student surface ───────────────────────────────────────────────────

    private static async Task<IResult> StudentListAsync(HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var sid = await StudentIdAsync(http, db, ct);
        if (sid is null) return Fail("no_student_record", StatusCodes.Status403Forbidden);
        return await ListForAsync(IntakeInstance.AudienceStudent, sid, null, db, ct);
    }

    private static async Task<IResult> StudentSaveAsync(
        Guid intakeInstanceId, [FromBody] SaveBody body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var sid = await StudentIdAsync(http, db, ct);
        if (sid is null) return Fail("no_student_record", StatusCodes.Status403Forbidden);
        return await SaveForAsync(intakeInstanceId, IntakeInstance.AudienceStudent, sid, null, body.AnswersJson, submit: false, http, db, ct);
    }

    private static async Task<IResult> StudentSubmitAsync(
        Guid intakeInstanceId, [FromBody] SaveBody body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var sid = await StudentIdAsync(http, db, ct);
        if (sid is null) return Fail("no_student_record", StatusCodes.Status403Forbidden);
        return await SaveForAsync(intakeInstanceId, IntakeInstance.AudienceStudent, sid, null, body.AnswersJson, submit: true, http, db, ct);
    }

    // ── Partner surface ───────────────────────────────────────────────────

    private static async Task<IResult> PartnerListAsync(HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var pid = await PartnerIdAsync(http, db, ct);
        if (pid is null) return Fail("no_partner_record", StatusCodes.Status403Forbidden);
        return await ListForAsync(IntakeInstance.AudiencePartner, null, pid, db, ct);
    }

    private static async Task<IResult> PartnerSaveAsync(
        Guid intakeInstanceId, [FromBody] SaveBody body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var pid = await PartnerIdAsync(http, db, ct);
        if (pid is null) return Fail("no_partner_record", StatusCodes.Status403Forbidden);
        return await SaveForAsync(intakeInstanceId, IntakeInstance.AudiencePartner, null, pid, body.AnswersJson, submit: false, http, db, ct);
    }

    private static async Task<IResult> PartnerSubmitAsync(
        Guid intakeInstanceId, [FromBody] SaveBody body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var pid = await PartnerIdAsync(http, db, ct);
        if (pid is null) return Fail("no_partner_record", StatusCodes.Status403Forbidden);
        return await SaveForAsync(intakeInstanceId, IntakeInstance.AudiencePartner, null, pid, body.AnswersJson, submit: true, http, db, ct);
    }
}
