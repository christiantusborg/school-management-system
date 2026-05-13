namespace School.PartnerAdminApi.Partner.V1.List.Endpoint;

public sealed class AdminPartnerV1ListEndpointResponseItem
{
    public required Guid PartnerId { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required int UserCount { get; init; }
    public required bool IsEnabled { get; init; }
    public DateTime? DisabledAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}
