namespace School.PartnerAdminApi.Partner.V1.Update.Endpoint;

[Route("/v1/admin/school/partners/{id:guid}")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromBody] AdminPartnerV1UpdateEndpointRequest request,
        [FromServices] OdinDbContext db,
        CancellationToken ct)
    {
        var partner = await db.Partners.FirstOrDefaultAsync(p => p.PartnerId == id, ct);
        if (partner is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name))
            partner.Name = request.Name.Trim();

        if (request.ContactPersonName  is not null) partner.ContactPersonName  = NullIfEmpty(request.ContactPersonName);
        if (request.ContactPersonTitle is not null) partner.ContactPersonTitle = NullIfEmpty(request.ContactPersonTitle);
        if (request.ContactPersonEmail is not null) partner.ContactPersonEmail = NullIfEmpty(request.ContactPersonEmail);
        if (request.ContactPersonPhone is not null) partner.ContactPersonPhone = NullIfEmpty(request.ContactPersonPhone);

        if (request.AddressLine1 is not null) partner.AddressLine1 = NullIfEmpty(request.AddressLine1);
        if (request.AddressLine2 is not null) partner.AddressLine2 = NullIfEmpty(request.AddressLine2);
        if (request.City         is not null) partner.City         = NullIfEmpty(request.City);
        if (request.StateRegion  is not null) partner.StateRegion  = NullIfEmpty(request.StateRegion);
        if (request.PostalCode   is not null) partner.PostalCode   = NullIfEmpty(request.PostalCode);
        if (request.Country      is not null) partner.Country      = NullIfEmpty(request.Country);

        if (request.Website            is not null) partner.Website            = NullIfEmpty(request.Website);
        if (request.RegistrationNumber is not null) partner.RegistrationNumber = NullIfEmpty(request.RegistrationNumber);
        if (request.TaxId              is not null) partner.TaxId              = NullIfEmpty(request.TaxId);

        if (request.ContractStart.HasValue) partner.ContractStart = request.ContractStart;
        if (request.ContractEnd.HasValue)   partner.ContractEnd   = request.ContractEnd;
        if (request.Tier          is not null) partner.Tier          = NullIfEmpty(request.Tier);
        if (request.InternalNotes is not null) partner.InternalNotes = NullIfEmpty(request.InternalNotes);

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private const string Route = "/v1/admin/school/partners/{id:guid}";

    private static string? NullIfEmpty(string? v)
        => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
