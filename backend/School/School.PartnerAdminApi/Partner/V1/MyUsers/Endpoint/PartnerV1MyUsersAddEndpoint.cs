namespace School.PartnerAdminApi.Partner.V1.MyUsers.Endpoint;

[Route("/v1/partner/my-users")]
[EndpointTag("Partner.MyUsers")]
public sealed class PartnerV1MyUsersAddEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        [FromBody] PartnerV1MyUsersAddRequest request,
        HttpContext httpContext,
        [FromServices] OdinDbContext db,
        [FromServices] OpaqueUserCreationService creator,
        CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        if (string.IsNullOrWhiteSpace(request.Username))
            return Results.BadRequest(new { error = "username_required" });

        try
        {
            var email = request.Email?.Trim() ?? $"{request.Username.Trim()}@partner.local";
            var (user, password) = await creator.CreateUserAsync(
                request.Username.Trim(), email, "Partner", partnerId!.Value, ct);

            return Results.Ok(new
            {
                userId = user.Id,
                username = user.UserName,
                temporaryPassword = password,
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private const string Route = "/v1/partner/my-users";
}

public sealed class PartnerV1MyUsersAddRequest
{
    public required string Username { get; init; }
    public string? Email { get; init; }
}
