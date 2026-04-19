using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;

namespace School.ProgrammeApi.Programme.V1.Reopen.Endpoint;

[Route("/v1/school/programmes/{id:guid}/reopen")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1ReopenEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] OdinDbContext db,
        CancellationToken ct)
    {
        var programme = await db.Programmes
            .FirstOrDefaultAsync(p => p.ProgrammeId == id && p.DeletedAt == null, ct);
        if (programme is null) return Results.NotFound();
        if (programme.PartnerId is null)
            return Results.Conflict(new { error = "core_programme_not_reopenable" });
        if (programme.Status != ProgrammeStatus.Rejected)
            return Results.Conflict(new { error = "not_rejected" });

        programme.Status = ProgrammeStatus.Pending;
        programme.RejectionReason = null;
        programme.SubmittedAt = DateTime.UtcNow;
        programme.ApprovedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private const string Route = "/v1/school/programmes/{id:guid}/reopen";
}
