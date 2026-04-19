using System.Security.Claims;

namespace School.PartnerAdminApi.Partner.V1.MyProgrammeAccess.Endpoint;

[Route("/v1/partner/programme-access")]
[EndpointTag("Partner.ProgrammeAccess")]
public sealed class PartnerV1MyProgrammeAccessEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, EndpointHandlerAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        [FromServices] OdinDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user?.PartnerId is null) return Results.Forbid();

        var partnerId = user.PartnerId.Value;

        var rows = await db.PartnerProgrammeAccesses
            .Where(a => a.PartnerId == partnerId && a.DeletedAt == null)
            .Join(db.Majors, a => a.MajorId, m => m.MajorId, (a, m) => new { a, m })
            .Join(db.Programmes, am => am.a.ProgrammeId, p => p.ProgrammeId,
                  (am, p) => new PartnerV1MyProgrammeAccessEndpointResponseItem
                  {
                      ProgrammeId       = p.ProgrammeId,
                      ProgrammeName     = p.Name,
                      MajorId           = am.m.MajorId,
                      MajorName         = am.m.Name,
                      DisabledByPartner = am.a.DisabledByPartner,
                  })
            .ToListAsync(ct);

        return Results.Ok(new PartnerV1MyProgrammeAccessEndpointResponse
        {
            Items = rows,
            Links = []
        });
    }

    private const string Route = "/v1/partner/programme-access";
}
