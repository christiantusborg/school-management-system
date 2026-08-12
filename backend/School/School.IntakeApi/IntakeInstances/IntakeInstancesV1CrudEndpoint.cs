using Odin.Api.Base.Authorization;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace School.IntakeApi.IntakeInstances;

/// <summary>
/// Admin CRUD for intake instances: a questionnaire template attached to a
/// fill audience (Student portal / Partner portal / SignupWizard survey
/// step). Also exposes the per-instance response list and single-response
/// read for the admin Responses tab. Routes live under the Admin-guarded
/// /v1/intake prefix.
/// </summary>
[Route("/v1/intake/intake-instances")]
[EndpointTag("Intake.Instances")]
public sealed class IntakeInstancesV1CrudEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/intake/intake-instances", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/intake-instances", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/intake/intake-instances/{intakeInstanceId:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/intake/intake-instances/{intakeInstanceId:guid}", SoftDeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/intake/intake-instances/{intakeInstanceId:guid}/restore", RestoreAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/intake-instances/{intakeInstanceId:guid}/responses", ListResponsesAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/intake-responses/{intakeResponseId:guid}", GetResponseAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/intake/intake-instances/{intakeInstanceId:guid}/assignments", GetAssignmentsAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/intake/intake-instances/{intakeInstanceId:guid}/assignments", SaveAssignmentsAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static readonly string[] ValidAudiences =
        [IntakeInstance.AudienceStudent, IntakeInstance.AudiencePartner, IntakeInstance.AudienceSignupWizard];

    public sealed class WriteRequest
    {
        public string? Name { get; init; }
        public string? Audience { get; init; }
        public bool IsActive { get; init; } = true;
        public Guid? QuestionnaireTemplateId { get; init; }
    }

    public sealed class AssignmentTarget
    {
        public Guid? StudentId { get; init; }
        public Guid? PartnerId { get; init; }
        public Guid? ProgrammeId { get; init; }
        public Guid? SpecializationId { get; init; }
        public Guid? SubjectId { get; init; }
    }

    public sealed class AssignmentsRequest
    {
        public string? AssignmentMode { get; init; }
        public List<AssignmentTarget>? Targets { get; init; }
    }

    private static IResult Ok(object data) => Results.Ok(new { success = true, data });
    private static IResult Fail(string error, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { success = false, error }, statusCode: status);

    private static async Task<IResult> ListAsync(
        OdinDbContext db, CancellationToken ct, bool includeDeleted = false)
    {
        var items = await db.IntakeInstances
            .Where(i => includeDeleted || i.DeletedAt == null)
            .OrderBy(i => i.DeletedAt != null)
            .ThenBy(i => i.Name)
            .Select(i => new
            {
                intakeInstanceId = i.IntakeInstanceId,
                name = i.Name,
                audience = i.Audience,
                assignmentMode = i.AssignmentMode,
                targetCount = db.IntakeAssignments.Count(a => a.IntakeInstanceId == i.IntakeInstanceId),
                isActive = i.IsActive,
                questionnaireTemplateId = i.QuestionnaireTemplateId,
                templateName = i.QuestionnaireTemplate != null ? i.QuestionnaireTemplate.Name : null,
                responseCount = db.IntakeResponses.Count(r => r.IntakeInstanceId == i.IntakeInstanceId && r.DeletedAt == null),
                submittedCount = db.IntakeResponses.Count(r => r.IntakeInstanceId == i.IntakeInstanceId
                    && r.DeletedAt == null && r.LifecycleState == IntakeResponseLifecycleState.Submitted),
                createdAt = i.CreatedAt,
                deletedAt = i.DeletedAt,
            })
            .ToListAsync(ct);
        return Ok(new { items });
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] WriteRequest body, HttpContext http, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "questionnaires.assign", ct) != AccessLevel.Edit) return Results.Forbid();

        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (!ValidAudiences.Contains(body.Audience)) return Fail("audience_invalid");
        if (body.QuestionnaireTemplateId is not { } tid
            || !await db.QuestionnaireTemplates.AnyAsync(t => t.QuestionnaireTemplateId == tid && t.DeletedAt == null, ct))
            return Fail("template_required");

        var entity = new IntakeInstance
        {
            IntakeInstanceId = Guid.NewGuid(),
            Name = body.Name.Trim(),
            Audience = body.Audience!,
            IsActive = body.IsActive,
            QuestionnaireTemplateId = tid,
            CreatedByUserId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        };
        db.IntakeInstances.Add(entity);
        await db.SaveChangesAsync(ct);
        return Ok(new { intakeInstanceId = entity.IntakeInstanceId });
    }

    private static async Task<IResult> UpdateAsync(
        Guid intakeInstanceId, [FromBody] WriteRequest body, HttpContext http, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "questionnaires.assign", ct) != AccessLevel.Edit) return Results.Forbid();

        var entity = await db.IntakeInstances
            .FirstOrDefaultAsync(i => i.IntakeInstanceId == intakeInstanceId && i.DeletedAt == null, ct);
        if (entity is null) return Fail("not_found", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(body.Name)) return Fail("name_required");
        if (!ValidAudiences.Contains(body.Audience)) return Fail("audience_invalid");
        if (body.QuestionnaireTemplateId is not { } tid
            || !await db.QuestionnaireTemplates.AnyAsync(t => t.QuestionnaireTemplateId == tid && t.DeletedAt == null, ct))
            return Fail("template_required");

        entity.Name = body.Name.Trim();
        entity.Audience = body.Audience!;
        entity.IsActive = body.IsActive;
        entity.QuestionnaireTemplateId = tid;
        entity.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { updated = true });
    }

    private static async Task<IResult> SoftDeleteAsync(
        Guid intakeInstanceId, HttpContext http, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "questionnaires.assign", ct) != AccessLevel.Edit) return Results.Forbid();

        var entity = await db.IntakeInstances
            .FirstOrDefaultAsync(i => i.IntakeInstanceId == intakeInstanceId && i.DeletedAt == null, ct);
        if (entity is null) return Fail("not_found", StatusCodes.Status404NotFound);
        entity.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { deleted = true });
    }

    private static async Task<IResult> RestoreAsync(
        Guid intakeInstanceId, HttpContext http, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "questionnaires.assign", ct) != AccessLevel.Edit) return Results.Forbid();

        var entity = await db.IntakeInstances
            .FirstOrDefaultAsync(i => i.IntakeInstanceId == intakeInstanceId && i.DeletedAt != null, ct);
        if (entity is null) return Fail("not_found", StatusCodes.Status404NotFound);
        entity.DeletedAt = null;
        entity.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { restored = true });
    }

    private static async Task<IResult> GetAssignmentsAsync(
        Guid intakeInstanceId, OdinDbContext db, CancellationToken ct)
    {
        var instance = await db.IntakeInstances
            .FirstOrDefaultAsync(i => i.IntakeInstanceId == intakeInstanceId && i.DeletedAt == null, ct);
        if (instance is null) return Fail("not_found", StatusCodes.Status404NotFound);

        var rows = await db.IntakeAssignments
            .Where(a => a.IntakeInstanceId == intakeInstanceId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        var items = new List<object>();
        foreach (var a in rows)
        {
            string kind; Guid target; string label;
            if (a.StudentId is { } sid)
            {
                kind = "student"; target = sid;
                var st = await db.Students.Where(s => s.StudentId == sid)
                    .Select(s => new { s.StudentNumber, s.UserId }).FirstOrDefaultAsync(ct);
                var prof = st is null ? null : await db.UserProfiles.Where(p => p.UserId == st.UserId)
                    .Select(p => new { p.FirstName, p.LastName }).FirstOrDefaultAsync(ct);
                label = st is null ? "(deleted student)"
                    : $"{prof?.FirstName} {prof?.LastName}".Trim() is { Length: > 0 } nm ? $"{nm} ({st.StudentNumber})" : st.StudentNumber ?? "student";
            }
            else if (a.PartnerId is { } pid)
            {
                kind = "partner"; target = pid;
                label = await db.Partners.Where(p => p.PartnerId == pid).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "(deleted partner)";
            }
            else if (a.ProgrammeId is { } prid)
            {
                kind = "programme"; target = prid;
                label = await db.Programmes.Where(p => p.ProgrammeId == prid).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "(deleted programme)";
            }
            else if (a.SpecializationId is { } spid)
            {
                kind = "specialization"; target = spid;
                label = await db.Specializations.Where(p => p.SpecializationId == spid).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "(deleted specialization)";
            }
            else if (a.SubjectId is { } suid)
            {
                kind = "subject"; target = suid;
                label = await db.Subjects.Where(p => p.SubjectId == suid).Select(p => p.Code + " " + p.Name).FirstOrDefaultAsync(ct) ?? "(deleted module)";
            }
            else continue;
            items.Add(new { intakeAssignmentId = a.IntakeAssignmentId, kind, targetId = target, label });
        }

        return Ok(new { assignmentMode = instance.AssignmentMode, items });
    }

    private static async Task<IResult> SaveAssignmentsAsync(
        Guid intakeInstanceId, [FromBody] AssignmentsRequest body, HttpContext http, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "questionnaires.assign", ct) != AccessLevel.Edit) return Results.Forbid();

        var instance = await db.IntakeInstances
            .FirstOrDefaultAsync(i => i.IntakeInstanceId == intakeInstanceId && i.DeletedAt == null, ct);
        if (instance is null) return Fail("not_found", StatusCodes.Status404NotFound);

        var mode = body.AssignmentMode == IntakeInstance.ModeTargeted
            ? IntakeInstance.ModeTargeted : IntakeInstance.ModeAudience;
        instance.AssignmentMode = mode;
        instance.ModifiedAt = DateTime.UtcNow;

        var existing = await db.IntakeAssignments
            .Where(a => a.IntakeInstanceId == intakeInstanceId).ToListAsync(ct);
        db.IntakeAssignments.RemoveRange(existing);

        foreach (var tgt in body.Targets ?? [])
        {
            var setCount = new[] { tgt.StudentId, tgt.PartnerId, tgt.ProgrammeId, tgt.SpecializationId, tgt.SubjectId }
                .Count(v => v is not null);
            if (setCount != 1) return Fail("each_target_needs_exactly_one_id");
            db.IntakeAssignments.Add(new IntakeAssignment
            {
                IntakeInstanceId = intakeInstanceId,
                StudentId = tgt.StudentId,
                PartnerId = tgt.PartnerId,
                ProgrammeId = tgt.ProgrammeId,
                SpecializationId = tgt.SpecializationId,
                SubjectId = tgt.SubjectId,
            });
        }
        await db.SaveChangesAsync(ct);
        return Ok(new { saved = true, assignmentMode = mode });
    }

    private static async Task<IResult> ListResponsesAsync(
        Guid intakeInstanceId, OdinDbContext db, CancellationToken ct)
    {
        var rows = await db.IntakeResponses
            .Where(r => r.IntakeInstanceId == intakeInstanceId && r.DeletedAt == null)
            .OrderByDescending(r => r.SubmittedAt ?? r.ModifiedAt)
            .Select(r => new
            {
                intakeResponseId = r.IntakeResponseId,
                lifecycleState = r.LifecycleState.ToString(),
                submittedAt = r.SubmittedAt,
                modifiedAt = r.ModifiedAt,
                questionnaireVersionHash = r.QuestionnaireVersionHash,
                studentNumber = r.StudentId != null
                    ? db.Students.Where(s => s.StudentId == r.StudentId).Select(s => s.StudentNumber).FirstOrDefault()
                    : null,
                studentName = r.StudentId != null
                    ? db.Students.Where(s => s.StudentId == r.StudentId)
                        .Select(s => db.UserProfiles.Where(p => p.UserId == s.UserId)
                            .Select(p => (p.FirstName + " " + p.LastName).Trim()).FirstOrDefault())
                        .FirstOrDefault()
                    : null,
                partnerName = r.PartnerId != null
                    ? db.Partners.Where(p => p.PartnerId == r.PartnerId).Select(p => p.Name).FirstOrDefault()
                    : null,
            })
            .ToListAsync(ct);
        return Ok(new { items = rows });
    }

    private static async Task<IResult> GetResponseAsync(
        Guid intakeResponseId, OdinDbContext db, CancellationToken ct)
    {
        var r = await db.IntakeResponses
            .Where(x => x.IntakeResponseId == intakeResponseId && x.DeletedAt == null)
            .Select(x => new
            {
                intakeResponseId = x.IntakeResponseId,
                intakeInstanceId = x.IntakeInstanceId,
                lifecycleState = x.LifecycleState.ToString(),
                submittedAt = x.SubmittedAt,
                questionnaireVersionHash = x.QuestionnaireVersionHash,
                answersJson = x.AnswersJson,
                definitionJson = x.IntakeInstance.QuestionnaireTemplate != null
                    ? x.IntakeInstance.QuestionnaireTemplate.DefinitionJson
                    : x.IntakeInstance.InlineDefinitionJson,
            })
            .FirstOrDefaultAsync(ct);
        if (r is null) return Fail("not_found", StatusCodes.Status404NotFound);
        return Ok(r);
    }
}
