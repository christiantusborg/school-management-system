namespace School.PartnerAdminApi.Partner.V1.List.Endpoint;

public sealed class AdminPartnerV1ListEndpointResponse : HateoasBaseResponse
{
    public required IReadOnlyList<AdminPartnerV1ListEndpointResponseItem> Items { get; init; }
    public required int Total { get; init; }
}
