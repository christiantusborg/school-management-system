namespace School.PartnerAdminApi.Partner.V1.ResetUserPassword.Endpoint;

[Route("/v1/admin/school/partners/{pid:guid}/users/{uid}/reset-password")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1ResetUserPasswordEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid pid, string uid,
        [FromServices] OdinDbContext db,
        [FromServices] OpaqueUserCreationService creator,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == uid && u.PartnerId == pid, ct);
        if (user is null) return Results.NotFound();

        var credentials = await db.OpaqueCredentials.Where(c => c.UserId == uid).ToListAsync(ct);
        db.OpaqueCredentials.RemoveRange(credentials);

        var kemKeys = await db.KemKeyPairs.Where(k => k.UserId == uid).ToListAsync(ct);
        db.KemKeyPairs.RemoveRange(kemKeys);

        var recoveryCodes = await db.OpaqueRecoveryCodes.Where(r => r.UserId == uid).ToListAsync(ct);
        db.OpaqueRecoveryCodes.RemoveRange(recoveryCodes);

        await db.SaveChangesAsync(ct);

        var password = await creator.RegenerateCredentialsAsync(user, ct);

        return Results.Ok(new
        {
            userId            = user.Id,
            username          = user.UserName,
            temporaryPassword = password
        });
    }

    private const string Route = "/v1/admin/school/partners/{pid:guid}/users/{uid}/reset-password";
}
