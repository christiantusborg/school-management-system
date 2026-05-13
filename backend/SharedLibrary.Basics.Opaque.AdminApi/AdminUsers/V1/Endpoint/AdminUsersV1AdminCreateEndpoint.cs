using Odin.Api.Base.Authorization;
using Odin.Api.Base.Data;
using Odin.Api.Base.Services;

namespace SharedLibrary.Basics.Opaque.AdminApi.AdminUsers.V1.Endpoint;

[Route("/v1/admin/admin-users")]
[EndpointTag("Admin.AdminUsers")]
public sealed class AdminUsersV1AdminCreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync).RequireAuthorization("SuperAdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        [FromBody] AdminUsersV1AdminCreateRequest request,
        HttpContext httpContext,
        [FromServices] OdinDbContext db,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] OpaqueUserCreationService creator,
        CancellationToken ct)
    {
        var (_, authFail) = await AdminUsersHelpers.RequireSuperAdminAsync(httpContext, userManager);
        if (authFail is not null) return authFail;

        if (string.IsNullOrWhiteSpace(request.Username))
            return Results.BadRequest(new { error = "username_required" });

        if (!AdminLevels.IsValid(request.Level))
            return Results.BadRequest(new { error = "invalid_level" });

        try
        {
            var email = request.Email?.Trim() ?? $"{request.Username.Trim()}@ibss.local";
            var (user, password) = await creator.CreateUserAsync(
                request.Username.Trim(), email, "Admin", partnerId: null, ct);

            await userManager.AddToRoleAsync(user, request.Level);

            return Results.Ok(new
            {
                userId = user.Id,
                username = user.UserName,
                email = user.Email,
                level = request.Level,
                temporaryPassword = password,
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private const string Route = "/v1/admin/admin-users";
}

public sealed class AdminUsersV1AdminCreateRequest
{
    public required string Username { get; init; }
    public string? Email { get; init; }
    public required string Level { get; init; }
}
