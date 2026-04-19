namespace School.PartnerAdminApi.Partner.V1.MyPrograms.Endpoint;

[Route("/v1/partner/my-programs/{id:guid}/submit")]
[EndpointTag("Partner.MyPrograms")]
public sealed class PartnerV1MyProgramsSubmitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] OdinDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var (_, partnerIdOrNull) = await MyProgramsHelpers.ResolvePartnerAsync(httpContext, db, ct);
        if (partnerIdOrNull is null) return Results.Forbid();
        var partnerId = partnerIdOrNull.Value;

        var programme = await db.Programmes
            .FirstOrDefaultAsync(p => p.ProgrammeId == id
                                   && p.PartnerId == partnerId
                                   && p.DeletedAt == null, ct);
        if (programme is null) return Results.NotFound();

        if (programme.IsDisabledByAdmin)
            return Results.Conflict(new { error = "disabled_by_admin" });

        if (programme.Status is not (ProgrammeStatus.Draft or ProgrammeStatus.Rejected))
            return Results.Conflict(new { error = "invalid_status" });

        programme.Status = ProgrammeStatus.Pending;
        programme.SubmittedAt = DateTime.UtcNow;
        programme.RejectionReason = null;

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private const string Route = "/v1/partner/my-programs/{id:guid}/submit";
}
