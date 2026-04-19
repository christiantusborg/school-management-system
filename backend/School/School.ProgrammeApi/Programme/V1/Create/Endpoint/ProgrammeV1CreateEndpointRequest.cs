namespace School.ProgrammeApi.Programme.V1.Create.Endpoint;

public sealed class ProgrammeV1CreateEndpointRequest
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public IReadOnlyList<int>? PathwayIds { get; init; }
}
