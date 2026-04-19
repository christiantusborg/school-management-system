using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;

namespace School.ProgrammeApi.Programme.V1.Approve.Endpoint;

[Route("/v1/school/programmes/{id:guid}/approve")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1ApproveEndpoint : IEndpointMarker
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
            return Results.Conflict(new { error = "core_programme_not_approvable" });
        if (programme.Status is not (ProgrammeStatus.Pending or ProgrammeStatus.Rejected))
            return Results.Conflict(new { error = "invalid_status" });

        programme.Status = ProgrammeStatus.Approved;
        programme.ApprovedAt = DateTime.UtcNow;
        programme.RejectionReason = null;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private const string Route = "/v1/school/programmes/{id:guid}/approve";
}
