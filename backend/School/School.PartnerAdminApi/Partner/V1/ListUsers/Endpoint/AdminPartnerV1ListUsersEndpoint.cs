namespace School.PartnerAdminApi.Partner.V1.ListUsers.Endpoint;

[Route("/v1/admin/school/partners/{id:guid}/users")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1ListUsersEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] OdinDbContext db,
        CancellationToken ct)
    {
        // Identify the Student role once so we can exclude student accounts
        // (those signed up via the public wizard) from the partner-org Users list.
        var studentRoleId = await db.Roles
            .Where(r => r.Name == "Student")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        var users = await db.Users
            .Where(u => u.PartnerId == id && u.DeletedAt == null)
            .Where(u => studentRoleId == null
                || !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == studentRoleId))
            .OrderBy(u => u.UserName)
            .Select(u => new
            {
                UserId    = u.Id,
                Username  = u.UserName,
                Email     = u.Email,
                IsEnabled = u.IsEnabled,
                CreatedAt = u.CreatedAt,
                FirstName = db.UserProfiles.Where(p => p.UserId == u.Id).Select(p => p.FirstName).FirstOrDefault(),
                LastName  = db.UserProfiles.Where(p => p.UserId == u.Id).Select(p => p.LastName).FirstOrDefault(),
            })
            .ToListAsync(ct);

        return Results.Ok(new { items = users, total = users.Count });
    }

    private const string Route = "/v1/admin/school/partners/{id:guid}/users";
}
