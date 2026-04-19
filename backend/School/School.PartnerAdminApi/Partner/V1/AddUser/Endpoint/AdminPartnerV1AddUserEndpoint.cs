namespace School.PartnerAdminApi.Partner.V1.AddUser.Endpoint;

[Route("/v1/admin/school/partners/{id:guid}/users")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1AddUserEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromBody] AdminPartnerV1AddUserEndpointRequest request,
        [FromServices] OdinDbContext db,
        [FromServices] OpaqueUserCreationService creator,
        CancellationToken ct)
    {
        var partner = await db.Partners.FirstOrDefaultAsync(p => p.PartnerId == id, ct);
        if (partner is null) return Results.NotFound("Partner not found.");

        if (string.IsNullOrWhiteSpace(request.Username))
            return Results.BadRequest("Username is required.");

        try
        {
            var email = request.Email?.Trim() ?? $"{request.Username.Trim()}@partner.local";
            var (user, password) = await creator.CreateUserAsync(
                request.Username.Trim(), email, "Partner", id, ct);

            return Results.Ok(new
            {
                userId            = user.Id,
                username          = user.UserName,
                temporaryPassword = password
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private const string Route = "/v1/admin/school/partners/{id:guid}/users";
}
