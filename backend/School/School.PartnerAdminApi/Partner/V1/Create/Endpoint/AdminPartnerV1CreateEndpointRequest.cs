namespace School.PartnerAdminApi.Partner.V1.Create.Endpoint;

public sealed class AdminPartnerV1CreateEndpointRequest
{
    public required string Name { get; init; }
    public required string Username { get; init; }
    public string? Email { get; init; }

    // Contact person
    public string? ContactPersonName { get; init; }
    public string? ContactPersonTitle { get; init; }
    public string? ContactPersonEmail { get; init; }
    public string? ContactPersonPhone { get; init; }

    // Address
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? StateRegion { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }

    // Organisation
    public string? Website { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? TaxId { get; init; }

    // Partnership metadata
    public DateTime? ContractStart { get; init; }
    public DateTime? ContractEnd { get; init; }
    public string? Tier { get; init; }
    public string? InternalNotes { get; init; }
}
