using Odin.Api.Base.Authentication;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// "Continue signup" from the admin Students list: mints a wizard token for a
/// student whose signup was never finished, so the Admission Office can open
/// the public wizard exactly where the applicant stopped — no password
/// needed (the caller is already an authenticated admin).
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/signup-token")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsSignupTokenEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/signup-token", HandleAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, OdinDbContext db, WizardSessionService wizard, CancellationToken ct)
    {
        var student = await db.Students
            .Where(s => s.StudentId == studentId && s.DeletedAt == null)
            .Select(s => new
            {
                s.UserId,
                s.WizardStep,
                PartnerSlug = db.Partners.Where(p => p.PartnerId == s.PartnerId).Select(p => p.Slug).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);
        if (student is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(student.PartnerSlug))
            return Results.BadRequest(new { error = "The student's partner has no slug — cannot open the wizard." });

        var wizardToken = await wizard.IssueAsync(student.UserId, studentId);
        return Results.Ok(new
        {
            wizardToken,
            partnerSlug = student.PartnerSlug,
            wizardStep = student.WizardStep,
        });
    }
}
