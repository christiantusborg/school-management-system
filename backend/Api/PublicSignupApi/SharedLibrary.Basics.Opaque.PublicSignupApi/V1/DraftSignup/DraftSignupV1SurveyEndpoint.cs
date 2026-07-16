using System.Text.Json;
using Odin.Api.Base.Authentication;
using SharedLibrary.Basics.Opaque.Domains.Intake;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Optional Survey step of the signup wizard, powered by the intake system:
/// active <see cref="IntakeInstance"/> rows with the SignupWizard audience.
/// GET returns the forms plus the wizard student's saved answers; PATCH
/// upserts a draft; the wizard's final submit does not depend on this step,
/// so an abandoned survey never blocks an application. Wizard-token auth,
/// same as every other draft-signup endpoint.
/// </summary>
[Route("/v1/public/draft-signup/survey")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1SurveyEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/public/draft-signup/survey", GetAsync).AllowAnonymous();
        app.MapPatch("/v1/public/draft-signup/survey/{intakeInstanceId:guid}", SaveAsync).AllowAnonymous();
        return app;
    }

    public sealed class SaveBody
    {
        public string? AnswersJson { get; init; }
        public bool Submit { get; init; }
    }

    private static async Task<IResult> GetAsync(
        HttpContext http, OdinDbContext db, WizardSessionService wizard, CancellationToken ct)
    {
        var session = await WizardTokenAuth.ResolveAsync(http, wizard);
        if (session is null) return Results.Unauthorized();

        var items = await db.IntakeInstances
            .Where(i => i.DeletedAt == null && i.IsActive
                && i.Audience == IntakeInstance.AudienceSignupWizard
                && i.QuestionnaireTemplate != null && i.QuestionnaireTemplate.DeletedAt == null)
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                intakeInstanceId = i.IntakeInstanceId,
                name = i.Name,
                definitionJson = i.QuestionnaireTemplate!.DefinitionJson,
                response = db.IntakeResponses
                    .Where(r => r.IntakeInstanceId == i.IntakeInstanceId
                        && r.StudentId == session.StudentId && r.DeletedAt == null)
                    .Select(r => new
                    {
                        lifecycleState = r.LifecycleState.ToString(),
                        answersJson = r.AnswersJson,
                        submittedAt = r.SubmittedAt,
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> SaveAsync(
        Guid intakeInstanceId, [FromBody] SaveBody body,
        HttpContext http, OdinDbContext db, WizardSessionService wizard, CancellationToken ct)
    {
        var session = await WizardTokenAuth.ResolveAsync(http, wizard);
        if (session is null) return Results.Unauthorized();

        var instance = await db.IntakeInstances
            .Include(i => i.QuestionnaireTemplate)
            .FirstOrDefaultAsync(i => i.IntakeInstanceId == intakeInstanceId
                && i.DeletedAt == null && i.IsActive
                && i.Audience == IntakeInstance.AudienceSignupWizard, ct);
        if (instance is null) return Results.NotFound();

        if (body.AnswersJson is not null)
        {
            try { using var _ = JsonDocument.Parse(body.AnswersJson); }
            catch { return Results.BadRequest(new { error = "answersJson is not valid JSON." }); }
        }

        var response = await db.IntakeResponses.FirstOrDefaultAsync(r =>
            r.IntakeInstanceId == intakeInstanceId && r.StudentId == session.StudentId && r.DeletedAt == null, ct);
        if (response is not null && response.LifecycleState == IntakeResponseLifecycleState.Submitted)
            return Results.Conflict(new { error = "Survey already submitted." });

        if (response is null)
        {
            response = new IntakeResponse
            {
                IntakeResponseId = Guid.NewGuid(),
                IntakeInstanceId = intakeInstanceId,
                StudentId = session.StudentId,
                CreatedByUserId = session.UserId,
            };
            db.IntakeResponses.Add(response);
        }
        if (body.AnswersJson is not null) response.AnswersJson = body.AnswersJson;
        response.ModifiedAt = DateTime.UtcNow;
        if (body.Submit)
        {
            response.LifecycleState = IntakeResponseLifecycleState.Submitted;
            response.SubmittedAt = DateTime.UtcNow;
            response.QuestionnaireVersionHash = instance.QuestionnaireTemplate?.DefinitionHash;
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { lifecycleState = response.LifecycleState.ToString(), submittedAt = response.SubmittedAt });
    }
}
