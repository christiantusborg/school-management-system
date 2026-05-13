using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner confirms a student's email address on their behalf — useful
/// when the student has lost the verification email or is signing up
/// in person at the partner's office. Flips
/// <c>ApplicationUser.EmailConfirmed</c> and the matching primary
/// <c>UserContactEmail.IsVerified</c> to true so the student can log in
/// without clicking the verify link.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/confirm-email")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsConfirmEmailEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-students/{studentId:guid}/confirm-email", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var student = await db.Students
            .Where(s => s.StudentId == studentId && s.PartnerId == partnerId && s.DeletedAt == null)
            .Select(s => new { s.UserId })
            .FirstOrDefaultAsync(ct);
        if (student is null) return Results.NotFound();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == student.UserId, ct);
        if (user is null) return Results.NotFound();

        user.EmailConfirmed = true;

        // Mirror onto the primary contact email so the email-list view in
        // the user-management screens stays consistent.
        var primaryEmail = await db.UserContactEmails
            .FirstOrDefaultAsync(e => e.UserId == student.UserId && e.IsPrimary, ct);
        if (primaryEmail is not null) primaryEmail.IsVerified = true;

        await db.SaveChangesAsync(ct);

        return Results.Ok(new { studentId, emailVerified = true });
    }
}
