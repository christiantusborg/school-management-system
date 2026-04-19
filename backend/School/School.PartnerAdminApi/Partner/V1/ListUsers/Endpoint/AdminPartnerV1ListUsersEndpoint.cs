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
        var users = await db.Users
            .Where(u => u.PartnerId == id)
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
