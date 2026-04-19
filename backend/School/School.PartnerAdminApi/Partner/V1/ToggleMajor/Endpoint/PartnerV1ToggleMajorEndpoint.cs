using System.Security.Claims;

namespace School.PartnerAdminApi.Partner.V1.ToggleMajor.Endpoint;

[Route("/v1/partner/programme-access/{majorId:guid}")]
[EndpointTag("Partner.ProgrammeAccess")]
public sealed class PartnerV1ToggleMajorEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch(Route, EndpointHandlerAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid majorId,
        [FromBody] PartnerV1ToggleMajorEndpointRequest request,
        [FromServices] OdinDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user?.PartnerId is null) return Results.Forbid();

        var row = await db.PartnerProgrammeAccesses
            .FirstOrDefaultAsync(a => a.PartnerId == user.PartnerId.Value
                                   && a.MajorId == majorId
                                   && a.DeletedAt == null, ct);
        if (row is null) return Results.NotFound();

        row.DisabledByPartner = request.Disabled;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private const string Route = "/v1/partner/programme-access/{majorId:guid}";
}
