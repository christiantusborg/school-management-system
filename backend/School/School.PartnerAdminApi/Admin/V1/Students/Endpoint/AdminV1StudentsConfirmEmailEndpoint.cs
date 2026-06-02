namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission Office confirms a student's email on their behalf. Mirrors
/// the partner-side endpoint: flips <c>ApplicationUser.EmailConfirmed</c>
/// and the primary <c>UserContactEmail.IsVerified</c>. No partner-
/// ownership gate.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/confirm-email")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsConfirmEmailEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/confirm-email", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, OdinDbContext db, CancellationToken ct)
    {
        var student = await db.Students
            .Where(s => s.StudentId == studentId && s.DeletedAt == null)
            .Select(s => new { s.UserId })
            .FirstOrDefaultAsync(ct);
        if (student is null) return Results.NotFound();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == student.UserId, ct);
        if (user is null) return Results.NotFound();

        user.EmailConfirmed = true;

        var primaryEmail = await db.UserContactEmails
            .FirstOrDefaultAsync(e => e.UserId == student.UserId && e.IsPrimary, ct);
        if (primaryEmail is not null) primaryEmail.IsVerified = true;

        await db.SaveChangesAsync(ct);

        return Results.Ok(new { studentId, emailVerified = true });
    }
}
