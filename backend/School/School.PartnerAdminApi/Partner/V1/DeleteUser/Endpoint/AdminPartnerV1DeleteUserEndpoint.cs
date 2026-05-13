using Microsoft.AspNetCore.Identity;

namespace School.PartnerAdminApi.Partner.V1.DeleteUser.Endpoint;

[Route("/v1/admin/school/partners/{pid:guid}/users/{uid}")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1DeleteUserEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(Route, EndpointHandlerAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid pid, string uid,
        [FromServices] OdinDbContext db,
        [FromServices] UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == uid && u.PartnerId == pid && u.DeletedAt == null, ct);
        if (user is null) return Results.NotFound();

        // Soft-delete: stamp DeletedAt and disable. Existing login flow already blocks
        // !IsEnabled, so this is sufficient to revoke access without touching their data.
        user.DeletedAt = DateTime.UtcNow;
        user.IsEnabled = false;
        await userManager.UpdateAsync(user);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private const string Route = "/v1/admin/school/partners/{pid:guid}/users/{uid}";
}
