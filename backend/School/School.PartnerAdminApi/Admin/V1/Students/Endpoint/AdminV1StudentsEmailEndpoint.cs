using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Change a student's email — allowed for both the Admission Office and the
/// student's own partner (teachers blocked by the partner write gate). The
/// login username follows when it equalled the old email (students sign in
/// with their email), uniqueness is enforced across all accounts, and the
/// address returns to unverified.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/email")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsEmailEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/admin/students/{studentId:guid}/email", AdminAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/partner/my-students/{studentId:guid}/email", PartnerAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class Body { public string? Email { get; init; } }

    private static async Task<IResult> AdminAsync(
        Guid studentId, [FromBody] Body body, OdinDbContext db,
        UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();
        return await ChangeAsync(student, body, userManager);
    }

    private static async Task<IResult> PartnerAsync(
        Guid studentId, [FromBody] Body body, HttpContext httpContext, OdinDbContext db,
        UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);
        var student = await db.Students.FirstOrDefaultAsync(s =>
            s.StudentId == studentId && (s.PartnerId == partnerId || s.Enrollments.Any(pe => pe.PartnerId == partnerId && pe.DeletedAt == null)) && s.DeletedAt == null, ct);
        if (student is null) return Results.NotFound();
        return await ChangeAsync(student, body, userManager);
    }

    private static async Task<IResult> ChangeAsync(
        Student student, Body body, UserManager<ApplicationUser> userManager)
    {
        var email = body.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || email.Contains(' '))
            return Results.BadRequest(new { error = "Enter a valid email address." });

        var user = await userManager.FindByIdAsync(student.UserId);
        if (user is null) return Results.NotFound();

        var taken = await userManager.FindByEmailAsync(email);
        if (taken is not null && taken.Id != user.Id)
            return Results.BadRequest(new { error = "That email already belongs to another account." });

        var oldEmail = user.Email;
        var usernameWasEmail = string.Equals(user.UserName, oldEmail, StringComparison.OrdinalIgnoreCase);

        var result = await userManager.SetEmailAsync(user, email);
        if (!result.Succeeded)
            return Results.BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

        // Students log in with their email — keep the username in sync when it
        // followed the old address.
        if (usernameWasEmail)
        {
            var unameResult = await userManager.SetUserNameAsync(user, email);
            if (!unameResult.Succeeded)
                return Results.BadRequest(new { error = string.Join("; ", unameResult.Errors.Select(e => e.Description)) });
        }

        return Results.Ok(new { email, username = user.UserName, emailVerified = user.EmailConfirmed });
    }
}
