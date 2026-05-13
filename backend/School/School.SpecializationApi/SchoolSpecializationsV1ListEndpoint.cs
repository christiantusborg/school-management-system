namespace School.SpecializationApi;

[Route("/v1/school/specializations")]
[EndpointTag("School.Specializations")]
public sealed class SchoolSpecializationsV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/school/specializations", HandleAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        OdinDbContext db,
        CancellationToken cancellationToken,
        [FromQuery] Guid? programmeId = null,
        [FromQuery] bool deleted = false)
    {
        var q = db.Specializations.AsQueryable();
        q = deleted
            ? q.Where(s => s.DeletedAt != null)
            : q.Where(s => s.DeletedAt == null);
        if (programmeId is not null)
            q = q.Where(s => s.ProgrammeId == programmeId);

        var items = await q
            .OrderBy(s => s.Code)
            .Select(s => new
            {
                specializationId = s.SpecializationId,
                programmeId = s.ProgrammeId,
                code = s.Code,
                name = s.Name,
                description = s.Description,
                durationOfStudyMonths = s.DurationOfStudyMonths,
                offerAcceptanceMode = s.OfferAcceptanceMode.ToString(),
                instructionLanguage = s.InstructionLanguage,
                deletedAt = s.DeletedAt,
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(new { items, total = items.Count });
    }
}
