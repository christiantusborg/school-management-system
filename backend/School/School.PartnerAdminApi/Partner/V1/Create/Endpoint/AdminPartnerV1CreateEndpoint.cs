namespace School.PartnerAdminApi.Partner.V1.Create.Endpoint;

[Route("/v1/admin/school/partners")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        [FromBody] AdminPartnerV1CreateEndpointRequest request,
        [FromServices] OpaqueUserCreationService creator,
        [FromServices] OdinDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Username))
            return Results.BadRequest("Name and Username are required.");

        // Pre-allocate PartnerId so the user can be tagged before Partner row exists.
        var partnerId = Guid.NewGuid();

        try
        {
            var email = request.Email?.Trim() ?? $"{request.Username.Trim()}@partner.local";
            var (user, password) = await creator.CreateUserAsync(
                request.Username.Trim(), email, "Partner", partnerId, ct);

            var partner = new SharedLibrary.Basics.Opaque.Domains.Partner
            {
                PartnerId = partnerId,
                Name      = request.Name.Trim(),
                UserId    = user.Id,

                ContactPersonName  = NullIfEmpty(request.ContactPersonName),
                ContactPersonTitle = NullIfEmpty(request.ContactPersonTitle),
                ContactPersonEmail = NullIfEmpty(request.ContactPersonEmail),
                ContactPersonPhone = NullIfEmpty(request.ContactPersonPhone),

                AddressLine1 = NullIfEmpty(request.AddressLine1),
                AddressLine2 = NullIfEmpty(request.AddressLine2),
                City         = NullIfEmpty(request.City),
                StateRegion  = NullIfEmpty(request.StateRegion),
                PostalCode   = NullIfEmpty(request.PostalCode),
                Country      = NullIfEmpty(request.Country),

                Website            = NullIfEmpty(request.Website),
                RegistrationNumber = NullIfEmpty(request.RegistrationNumber),
                TaxId              = NullIfEmpty(request.TaxId),

                ContractStart = request.ContractStart,
                ContractEnd   = request.ContractEnd,
                Tier          = NullIfEmpty(request.Tier),
                InternalNotes = NullIfEmpty(request.InternalNotes),
            };
            db.Partners.Add(partner);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new AdminPartnerV1CreateEndpointResponse
            {
                PartnerId         = partner.PartnerId,
                Username          = user.UserName!,
                TemporaryPassword = password,
                Links             = []
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private const string Route = "/v1/admin/school/partners";

    private static string? NullIfEmpty(string? v)
        => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
