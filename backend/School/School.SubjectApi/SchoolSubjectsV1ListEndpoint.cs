namespace School.SubjectApi;

[Route("/v1/school/subjects")]
[EndpointTag("School.Subjects")]
public sealed class SchoolSubjectsV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/school/subjects", HandleAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        OdinDbContext db,
        CancellationToken cancellationToken,
        [FromQuery] Guid? specializationId = null,
        [FromQuery] bool deleted = false)
    {
        var q = db.Subjects.AsQueryable();
        q = deleted
            ? q.Where(s => s.DeletedAt != null)
            : q.Where(s => s.DeletedAt == null);
        if (specializationId is not null)
            q = q.Where(s => s.SpecializationId == specializationId);

        var items = await q
            .OrderBy(s => s.Code)
            .Select(s => new
            {
                subjectId = s.SubjectId,
                specializationId = s.SpecializationId,
                code = s.Code,
                name = s.Name,
                description = s.Description,
                ects = s.Ects,
                deletedAt = s.DeletedAt,
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(new { items, total = items.Count });
    }
}
