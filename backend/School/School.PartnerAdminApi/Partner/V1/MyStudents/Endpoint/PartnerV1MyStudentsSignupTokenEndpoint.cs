using Odin.Api.Base.Authentication;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner-portal "Continue signup": mints a wizard token for one of the
/// partner's OWN students whose signup was never finished, so partner staff
/// can open the public wizard exactly where the applicant stopped — no
/// password needed. Mirrors the admin endpoint with an ownership check;
/// teacher accounts are blocked by the partner write gate (POST).
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/signup-token")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsSignupTokenEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-students/{studentId:guid}/signup-token", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, HttpContext httpContext, OdinDbContext db,
        WizardSessionService wizard, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);

        var student = await db.Students
            .Where(s => s.StudentId == studentId && (s.PartnerId == partnerId || s.Enrollments.Any(pe => pe.PartnerId == partnerId && pe.DeletedAt == null)) && s.DeletedAt == null)
            .Select(s => new
            {
                s.UserId,
                s.WizardStep,
                PartnerSlug = db.Partners.Where(p => p.PartnerId == s.PartnerId).Select(p => p.Slug).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);
        if (student is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(student.PartnerSlug))
            return Results.BadRequest(new { error = "This partner has no slug — cannot open the wizard." });

        var wizardToken = await wizard.IssueAsync(student.UserId, studentId);
        return Results.Ok(new
        {
            wizardToken,
            partnerSlug = student.PartnerSlug,
            wizardStep = student.WizardStep,
        });
    }
}
