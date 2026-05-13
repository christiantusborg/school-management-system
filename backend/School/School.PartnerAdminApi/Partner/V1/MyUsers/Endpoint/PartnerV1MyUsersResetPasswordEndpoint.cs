namespace School.PartnerAdminApi.Partner.V1.MyUsers.Endpoint;

[Route("/v1/partner/my-users/{uid}/reset-password")]
[EndpointTag("Partner.MyUsers")]
public sealed class PartnerV1MyUsersResetPasswordEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        string uid,
        HttpContext httpContext,
        [FromServices] OdinDbContext db,
        [FromServices] OpaqueUserCreationService creator,
        CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.Id == uid && u.PartnerId == partnerId && u.DeletedAt == null, ct);
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
            userId = user.Id,
            username = user.UserName,
            temporaryPassword = password,
        });
    }

    private const string Route = "/v1/partner/my-users/{uid}/reset-password";
}
