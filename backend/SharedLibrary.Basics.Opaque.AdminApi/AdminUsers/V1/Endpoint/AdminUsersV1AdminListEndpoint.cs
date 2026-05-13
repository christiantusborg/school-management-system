using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Data;

namespace SharedLibrary.Basics.Opaque.AdminApi.AdminUsers.V1.Endpoint;

[Route("/v1/admin/admin-users")]
[EndpointTag("Admin.AdminUsers")]
public sealed class AdminUsersV1AdminListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, EndpointHandlerAsync).RequireAuthorization("SuperAdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        HttpContext httpContext,
        [FromServices] OdinDbContext db,
        [FromServices] UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        var (currentUserId, fail) = await AdminUsersHelpers.RequireSuperAdminAsync(httpContext, userManager);
        if (fail is not null) return fail;

        // Hit the role join directly so we don't iterate every user in the
        // system. `UserRoles` + `Roles` gives us the admins in one query.
        var adminUserIds = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where r.Name == "Admin"
            select ur.UserId
        ).ToListAsync(ct);

        var users = await db.Users
            .Where(u => adminUserIds.Contains(u.Id) && u.DeletedAt == null)
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);

        var items = new List<object>(users.Count);
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            items.Add(new
            {
                userId = u.Id,
                username = u.UserName,
                email = u.Email,
                isEnabled = u.IsEnabled,
                level = AdminUsersHelpers.PickLevel(roles),
                isSelf = u.Id == currentUserId,
                createdAt = u.CreatedAt,
            });
        }

        return Results.Ok(new { items });
    }

    private const string Route = "/v1/admin/admin-users";
}
