namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin resets a student's login password. Wipes OPAQUE credentials +
/// KEM keys + recovery codes and regenerates via
/// <see cref="OpaqueUserCreationService"/>. Accepts an optional custom
/// password in the body; blank → server generates a random one.
/// Mirrors <see cref="AdminPartnerV1ResetUserPasswordEndpoint"/> shape.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/reset-password")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsResetPasswordEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/{studentId:guid}/reset-password", HandleAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class ResetRequest
    {
        public string? Password { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        [FromBody] ResetRequest? body,
        [FromServices] OdinDbContext db,
        [FromServices] OpaqueUserCreationService creator,
        CancellationToken ct)
    {
        var student = await db.Students
            .Where(s => s.StudentId == studentId && s.DeletedAt == null)
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
