namespace School.PartnerAdminApi.Partner.V1.Create.Endpoint;

public sealed class AdminPartnerV1CreateEndpointResponse : HateoasBaseResponse
{
    public required Guid PartnerId { get; init; }
    public required string Username { get; init; }
    public required string TemporaryPassword { get; init; }
}
