using Odin.Api.Base.Authorization;

namespace SharedLibrary.Basics.Opaque.AdminApi.AdminUsers.V1.Endpoint;

[Route("/v1/admin/admin-levels")]
[EndpointTag("Admin.AdminUsers")]
public sealed class AdminUsersV1AdminLevelsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, EndpointHandlerAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        HttpContext httpContext,
        [FromServices] UserManager<ApplicationUser> userManager)
    {
        var (_, authFail) = await AdminUsersHelpers.RequireAdminAsync(httpContext, userManager);
        if (authFail is not null) return authFail;
        return Results.Ok(new { items = AdminLevels.All });
    }

    private const string Route = "/v1/admin/admin-levels";
}
