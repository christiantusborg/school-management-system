namespace School.PartnerAdminApi.Partner.V1.GrantProgrammeAccess.Endpoint;

[Route("/v1/admin/school/partners/{id:guid}/programme-access")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1GrantProgrammeAccessEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromBody] AdminPartnerV1GrantProgrammeAccessEndpointRequest request,
        [FromServices] OdinDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var partner = await db.Partners.FirstOrDefaultAsync(p => p.PartnerId == id, ct);
        if (partner is null) return Results.NotFound();

        if (request.MajorIds.Count == 0) return Results.Ok();

        var majorIds = request.MajorIds.Distinct().ToList();

        var majors = await db.Majors
            .Where(m => majorIds.Contains(m.MajorId))
            .Select(m => new { m.MajorId, m.ProgrammeId })
            .ToListAsync(ct);

        if (majors.Count != majorIds.Count)
            return Results.BadRequest("One or more majorIds are invalid.");

        var existing = await db.PartnerProgrammeAccesses
            .Where(a => a.PartnerId == id && majorIds.Contains(a.MajorId))
            .ToListAsync(ct);

        var grantedBy = httpContext.User.Identity?.Name;
        var now = DateTime.UtcNow;

        foreach (var m in majors)
        {
            var row = existing.FirstOrDefault(a => a.MajorId == m.MajorId);
            if (row is null)
            {
                db.PartnerProgrammeAccesses.Add(new PartnerProgrammeAccess
                {
                    PartnerId         = id,
                    ProgrammeId       = m.ProgrammeId,
                    MajorId           = m.MajorId,
                    GrantedAt         = now,
                    GrantedByUserId   = grantedBy,
                    DisabledByPartner = false,
                });
            }
            else if (row.DeletedAt != null)
            {
                row.DeletedAt         = null;
                row.GrantedAt         = now;
                row.GrantedByUserId   = grantedBy;
                row.DisabledByPartner = false;
            }
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private const string Route = "/v1/admin/school/partners/{id:guid}/programme-access";
}
