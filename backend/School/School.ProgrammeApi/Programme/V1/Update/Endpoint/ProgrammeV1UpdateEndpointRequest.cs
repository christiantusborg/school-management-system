namespace School.ProgrammeApi.Programme.V1.Update.Endpoint;

public sealed class ProgrammeV1UpdateEndpointRequest
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public IReadOnlyList<int>? PathwayIds { get; init; }
}
