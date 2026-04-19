namespace School.PartnerAdminApi.Partner.V1.Get.Endpoint;

[Route("/v1/admin/school/partners/{id:guid}")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1GetEndpoint : IEndpointMarker
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
        var partner = await db.Partners
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PartnerId == id, ct);
        if (partner is null) return Results.NotFound();

        return Results.Ok(new AdminPartnerV1GetEndpointResponse
        {
            PartnerId          = partner.PartnerId,
            Name               = partner.Name,
            IsEnabled          = partner.DeletedAt is null,
            ContactPersonName  = partner.ContactPersonName,
            ContactPersonTitle = partner.ContactPersonTitle,
            ContactPersonEmail = partner.ContactPersonEmail,
            ContactPersonPhone = partner.ContactPersonPhone,
            AddressLine1       = partner.AddressLine1,
            AddressLine2       = partner.AddressLine2,
            City               = partner.City,
            StateRegion        = partner.StateRegion,
            PostalCode         = partner.PostalCode,
            Country            = partner.Country,
            Website            = partner.Website,
            RegistrationNumber = partner.RegistrationNumber,
            TaxId              = partner.TaxId,
            ContractStart      = partner.ContractStart,
            ContractEnd        = partner.ContractEnd,
            Tier               = partner.Tier,
            InternalNotes      = partner.InternalNotes,
            Links              = []
        });
    }

    private const string Route = "/v1/admin/school/partners/{id:guid}";
}
