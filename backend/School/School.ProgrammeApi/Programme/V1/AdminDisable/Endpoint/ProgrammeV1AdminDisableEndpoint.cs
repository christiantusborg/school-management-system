using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;

namespace School.ProgrammeApi.Programme.V1.AdminDisable.Endpoint;

[Route("/v1/school/programmes/{id:guid}/admin-disable")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1AdminDisableEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromBody] ProgrammeV1AdminDisableEndpointRequest request,
        [FromServices] OdinDbContext db,
        CancellationToken ct)
    {
        var programme = await db.Programmes
            .FirstOrDefaultAsync(p => p.ProgrammeId == id && p.DeletedAt == null, ct);
        if (programme is null) return Results.NotFound();
        if (programme.PartnerId is null)
            return Results.Conflict(new { error = "core_programme_not_disableable" });

        programme.IsDisabledByAdmin = request.Disabled;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private const string Route = "/v1/school/programmes/{id:guid}/admin-disable";
}

public sealed class ProgrammeV1AdminDisableEndpointRequest
{
    public required bool Disabled { get; init; }
}
