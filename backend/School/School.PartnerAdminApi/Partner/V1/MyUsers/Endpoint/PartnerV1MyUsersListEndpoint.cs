namespace School.PartnerAdminApi.Partner.V1.MyUsers.Endpoint;

[Route("/v1/partner/my-users")]
[EndpointTag("Partner.MyUsers")]
public sealed class PartnerV1MyUsersListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, EndpointHandlerAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        HttpContext httpContext,
        [FromServices] OdinDbContext db,
        CancellationToken ct)
    {
        var (callerId, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var studentRoleId = await db.Roles
            .Where(r => r.Name == "Student")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        var users = await db.Users
            .Where(u => u.PartnerId == partnerId && u.DeletedAt == null)
            .Where(u => studentRoleId == null
                || !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == studentRoleId))
            .OrderBy(u => u.UserName)
            .Select(u => new
            {
                userId    = u.Id,
                username  = u.UserName,
                email     = u.Email,
                isEnabled = u.IsEnabled,
                isSelf    = u.Id == callerId,
                createdAt = u.CreatedAt,
                firstName = db.UserProfiles.Where(p => p.UserId == u.Id).Select(p => p.FirstName).FirstOrDefault(),
                lastName  = db.UserProfiles.Where(p => p.UserId == u.Id).Select(p => p.LastName).FirstOrDefault(),
            })
            .ToListAsync(ct);

        return Results.Ok(new { items = users, total = users.Count });
    }

    private const string Route = "/v1/partner/my-users";
}
