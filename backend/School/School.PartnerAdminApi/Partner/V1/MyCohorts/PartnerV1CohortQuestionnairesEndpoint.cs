using School.PartnerAdminApi.Admin.V1.ModuleCohorts;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyCohorts;

/// <summary>
/// Partner/teacher view of a cohort's questionnaire results: the anonymous
/// aggregate only, and each questionnaire stays locked until it has at least
/// 3 responses so a single student's answers can never be singled out
/// (below that, only the Admission Office can see the aggregate).
/// Attaching/removing questionnaires is Admission-only.
/// </summary>
[Route("/v1/partner/my/cohorts/{cohortId}/questionnaires")]
[EndpointTag("Partner.MyCohorts")]
public sealed class PartnerV1CohortQuestionnairesEndpoint : IEndpointMarker
{
    private const int MinResponsesForStaff = 3;

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my/cohorts/{cohortId:guid}/questionnaires/stats", StatsAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> StatsAsync(
        Guid cohortId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var owned = await db.ModuleCohorts.AnyAsync(c =>
            c.ModuleCohortId == cohortId && c.PartnerId == partnerId && c.DeletedAt == null, ct);
        if (!owned) return Results.NotFound();

        return Results.Ok(await CohortQuestionnaireStats.BuildAsync(
            db, cohortId, minResponses: MinResponsesForStaff, ct));
    }
}
