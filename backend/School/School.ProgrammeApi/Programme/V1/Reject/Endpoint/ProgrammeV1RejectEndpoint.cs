using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;

namespace School.ProgrammeApi.Programme.V1.Reject.Endpoint;

[Route("/v1/school/programmes/{id:guid}/reject")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1RejectEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromBody] ProgrammeV1RejectEndpointRequest request,
        [FromServices] OdinDbContext db,
        CancellationToken ct)
    {
        var reason = (request.Reason ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return Results.BadRequest(new { error = "reason_required" });

        var programme = await db.Programmes
            .FirstOrDefaultAsync(p => p.ProgrammeId == id && p.DeletedAt == null, ct);
        if (programme is null) return Results.NotFound();
        if (programme.PartnerId is null)
            return Results.Conflict(new { error = "core_programme_not_rejectable" });
        if (programme.Status != ProgrammeStatus.Pending)
            return Results.Conflict(new { error = "invalid_status" });

        programme.Status = ProgrammeStatus.Rejected;
        programme.RejectionReason = reason;
        programme.ApprovedAt = null;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private const string Route = "/v1/school/programmes/{id:guid}/reject";
}

public sealed class ProgrammeV1RejectEndpointRequest
{
    public required string Reason { get; init; }
}
