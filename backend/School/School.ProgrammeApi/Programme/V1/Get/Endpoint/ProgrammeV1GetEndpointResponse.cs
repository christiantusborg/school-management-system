namespace School.ProgrammeApi.Programme.V1.Get.Endpoint;

public sealed class ProgrammeV1GetEndpointResponse : HateoasBaseResponse
{
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required IReadOnlyList<int> PathwayIds { get; init; }
    public DateTime? DeletedAt { get; init; }
}
