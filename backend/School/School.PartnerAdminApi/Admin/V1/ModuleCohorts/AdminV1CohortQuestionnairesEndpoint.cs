using Odin.Api.Base.Authorization;

namespace School.PartnerAdminApi.Admin.V1.ModuleCohorts;

/// <summary>
/// Admission Office management of the questionnaires attached to a module
/// cohort (the cohort dialog's Questionnaires tab). Templates come from the
/// existing Questionnaires builder; any number can be attached per cohort.
/// Students must submit them all before the cohort grade shows in their
/// portal. Admin also gets the anonymous aggregate with NO minimum-response
/// threshold (partner/teacher stats are served by the partner endpoint and
/// lock below 3 responses).
/// </summary>
[Route("/v1/admin/cohorts/{cohortId}/questionnaires")]
[EndpointTag("Admin.ModuleCohorts")]
public sealed class AdminV1CohortQuestionnairesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/cohorts/{cohortId:guid}/questionnaires", ListAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/cohorts/{cohortId:guid}/questionnaires", AttachAsync)
            .RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/cohort-questionnaires/{id:guid}", DetachAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/cohorts/{cohortId:guid}/questionnaires/stats", StatsAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> ListAsync(
        Guid cohortId, OdinDbContext db, CancellationToken ct)
    {
        var items = await (
            from q in db.ModuleCohortQuestionnaires
            join t in db.QuestionnaireTemplates on q.QuestionnaireTemplateId equals t.QuestionnaireTemplateId
            where q.ModuleCohortId == cohortId && q.DeletedAt == null
            orderby q.SortOrder, q.CreatedAt
            select new
            {
                moduleCohortQuestionnaireId = q.ModuleCohortQuestionnaireId,
                questionnaireTemplateId = q.QuestionnaireTemplateId,
                name = t.Name,
                responses = db.CohortQuestionnaireResponses
                    .Count(r => r.ModuleCohortQuestionnaireId == q.ModuleCohortQuestionnaireId),
                completed = db.CohortQuestionnaireCompletions
                    .Count(c => c.ModuleCohortQuestionnaireId == q.ModuleCohortQuestionnaireId),
            }).ToListAsync(ct);

        var assigned = await db.ModuleCohortStudents
            .CountAsync(s => s.ModuleCohortId == cohortId && s.DeletedAt == null, ct);
        return Results.Ok(new { assigned, items });
    }

    private sealed class AttachBody
    {
        public Guid? QuestionnaireTemplateId { get; init; }
    }

    private static async Task<IResult> AttachAsync(
        Guid cohortId, [FromBody] AttachBody body, HttpContext httpContext, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "cohorts.assign_questionnaire", ct) != AccessLevel.Edit) return Results.Forbid();

        if (body.QuestionnaireTemplateId is not { } templateId)
            return Results.BadRequest(new { error = "questionnaireTemplateId is required." });

        var cohortExists = await db.ModuleCohorts
            .AnyAsync(c => c.ModuleCohortId == cohortId && c.DeletedAt == null, ct);
        if (!cohortExists) return Results.NotFound();

        var template = await db.QuestionnaireTemplates
            .AnyAsync(t => t.QuestionnaireTemplateId == templateId && t.DeletedAt == null, ct);
        if (!template) return Results.BadRequest(new { error = "Questionnaire template not found." });

        var duplicate = await db.ModuleCohortQuestionnaires.AnyAsync(q =>
            q.ModuleCohortId == cohortId && q.QuestionnaireTemplateId == templateId && q.DeletedAt == null, ct);
        if (duplicate)
            return Results.BadRequest(new { error = "That questionnaire is already attached to this cohort." });

        var maxSort = await db.ModuleCohortQuestionnaires
            .Where(q => q.ModuleCohortId == cohortId && q.DeletedAt == null)
            .Select(q => (int?)q.SortOrder).MaxAsync(ct) ?? 0;

        var row = new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.ModuleCohortQuestionnaire
        {
            ModuleCohortId = cohortId,
            QuestionnaireTemplateId = templateId,
            SortOrder = maxSort + 1,
        };
        db.ModuleCohortQuestionnaires.Add(row);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { moduleCohortQuestionnaireId = row.ModuleCohortQuestionnaireId });
    }

    private static async Task<IResult> DetachAsync(
        Guid id, HttpContext httpContext, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "cohorts.assign_questionnaire", ct) != AccessLevel.Edit) return Results.Forbid();

        var row = await db.ModuleCohortQuestionnaires
            .FirstOrDefaultAsync(q => q.ModuleCohortQuestionnaireId == id && q.DeletedAt == null, ct);
        if (row is null) return Results.NotFound();
        row.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { removed = true });
    }

    private static async Task<IResult> StatsAsync(
        Guid cohortId, OdinDbContext db, CancellationToken ct)
    {
        return Results.Ok(await CohortQuestionnaireStats.BuildAsync(db, cohortId, minResponses: null, ct));
    }
}
