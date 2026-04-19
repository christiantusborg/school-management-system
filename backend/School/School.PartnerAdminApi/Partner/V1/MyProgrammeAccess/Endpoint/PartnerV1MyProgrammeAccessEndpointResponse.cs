namespace School.PartnerAdminApi.Partner.V1.MyProgrammeAccess.Endpoint;

public sealed class PartnerV1MyProgrammeAccessEndpointResponse : HateoasBaseResponse
{
    public required IReadOnlyList<PartnerV1MyProgrammeAccessEndpointResponseItem> Items { get; init; }
}

public sealed class PartnerV1MyProgrammeAccessEndpointResponseItem
{
    public required Guid ProgrammeId { get; init; }
    public required string ProgrammeName { get; init; }
    public required Guid MajorId { get; init; }
    public required string MajorName { get; init; }
    public required bool DisabledByPartner { get; init; }
}
