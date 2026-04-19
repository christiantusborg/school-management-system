namespace School.PartnerAdminApi.Partner.V1.Get.Endpoint;

public sealed class AdminPartnerV1GetEndpointResponse : HateoasBaseResponse
{
    public required Guid PartnerId { get; init; }
    public required string Name { get; init; }
    public required bool IsEnabled { get; init; }

    public string? ContactPersonName { get; init; }
    public string? ContactPersonTitle { get; init; }
    public string? ContactPersonEmail { get; init; }
    public string? ContactPersonPhone { get; init; }

    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? StateRegion { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }

    public string? Website { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? TaxId { get; init; }

    public DateTime? ContractStart { get; init; }
    public DateTime? ContractEnd { get; init; }
    public string? Tier { get; init; }
    public string? InternalNotes { get; init; }
}
