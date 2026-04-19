namespace School.PartnerAdminApi.Partner.V1.ListProgrammeAccess.Endpoint;

public sealed class AdminPartnerV1ListProgrammeAccessEndpointResponse : HateoasBaseResponse
{
    public required IReadOnlyList<AdminPartnerV1ListProgrammeAccessEndpointResponseItem> Items { get; init; }
}

public sealed class AdminPartnerV1ListProgrammeAccessEndpointResponseItem
{
    public required Guid ProgrammeId { get; init; }
    public required string ProgrammeName { get; init; }
    public required Guid MajorId { get; init; }
    public required string MajorName { get; init; }
    public required DateTime GrantedAt { get; init; }
    public required bool DisabledByPartner { get; init; }
}
