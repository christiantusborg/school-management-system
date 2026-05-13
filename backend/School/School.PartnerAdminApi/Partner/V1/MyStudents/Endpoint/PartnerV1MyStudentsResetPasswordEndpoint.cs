using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner resets one of their own students' login passwords. Same
/// credential rotation as the admin variant, but scoped via the
/// partner-owns-student ownership check.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/reset-password")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsResetPasswordEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/partner/my-students/{studentId:guid}/reset-password", HandleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class ResetRequest
    {
        public string? Password { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        [FromBody] ResetRequest? body,
        HttpContext httpContext,
        [FromServices] OdinDbContext db,
        [FromServices] OpaqueUserCreationService creator,
        CancellationToken ct)
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

        db.OpaqueCredentials.RemoveRange(await db.OpaqueCredentials.Where(c => c.UserId == user.Id).ToListAsync(ct));
        db.KemKeyPairs.RemoveRange(await db.KemKeyPairs.Where(k => k.UserId == user.Id).ToListAsync(ct));
        db.OpaqueRecoveryCodes.RemoveRange(await db.OpaqueRecoveryCodes.Where(r => r.UserId == user.Id).ToListAsync(ct));
        await db.SaveChangesAsync(ct);

        var customPassword = string.IsNullOrWhiteSpace(body?.Password) ? null : body!.Password;
        var password = await creator.RegenerateCredentialsAsync(user, ct, customPassword);

        return Results.Ok(new
        {
            userId            = user.Id,
            username          = user.UserName,
            temporaryPassword = password,
        });
    }
}
