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
        [FromBody] WriteRequest body, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
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
        Guid intakeInstanceId, [FromBody] WriteRequest body, OdinDbContext db, CancellationToken ct)
    {
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
        Guid intakeInstanceId, OdinDbContext db, CancellationToken ct)
    {
        var entity = await db.IntakeInstances
            .FirstOrDefaultAsync(i => i.IntakeInstanceId == intakeInstanceId && i.DeletedAt == null, ct);
        if (entity is null) return Fail("not_found", StatusCodes.Status404NotFound);
        entity.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { deleted = true });
    }

    private static async Task<IResult> RestoreAsync(
        Guid intakeInstanceId, OdinDbContext db, CancellationToken ct)
    {
        var entity = await db.IntakeInstances
            .FirstOrDefaultAsync(i => i.IntakeInstanceId == intakeInstanceId && i.DeletedAt != null, ct);
        if (entity is null) return Fail("not_found", StatusCodes.Status404NotFound);
        entity.DeletedAt = null;
        entity.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { restored = true });
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
