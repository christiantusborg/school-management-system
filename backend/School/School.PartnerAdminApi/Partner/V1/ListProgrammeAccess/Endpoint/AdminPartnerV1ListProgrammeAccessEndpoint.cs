namespace School.PartnerAdminApi.Partner.V1.ListProgrammeAccess.Endpoint;

[Route("/v1/admin/school/partners/{id:guid}/programme-access")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1ListProgrammeAccessEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] OdinDbContext db,
        CancellationToken ct)
    {
        var exists = await db.Partners.AnyAsync(p => p.PartnerId == id, ct);
        if (!exists) return Results.NotFound();

        var rows = await db.PartnerProgrammeAccesses
            .Where(a => a.PartnerId == id && a.DeletedAt == null)
            .Join(db.Majors, a => a.MajorId, m => m.MajorId, (a, m) => new { a, m })
            .Join(db.Programmes, am => am.a.ProgrammeId, p => p.ProgrammeId,
                  (am, p) => new AdminPartnerV1ListProgrammeAccessEndpointResponseItem
                  {
                      ProgrammeId       = p.ProgrammeId,
                      ProgrammeName     = p.Name,
                      MajorId           = am.m.MajorId,
                      MajorName         = am.m.Name,
                      GrantedAt         = am.a.GrantedAt,
                      DisabledByPartner = am.a.DisabledByPartner,
                  })
            .ToListAsync(ct);

        return Results.Ok(new AdminPartnerV1ListProgrammeAccessEndpointResponse
        {
            Items = rows,
            Links = []
        });
    }

    private const string Route = "/v1/admin/school/partners/{id:guid}/programme-access";
}
