namespace School.PartnerAdminApi.Partner.V1.GrantProgrammeAccess.Endpoint;

public sealed class AdminPartnerV1GrantProgrammeAccessEndpointRequest
{
    public required IReadOnlyList<Guid> MajorIds { get; init; }
}
