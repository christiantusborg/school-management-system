namespace School.MajorApi.Major.V1.Create.Endpoint;

public sealed class MajorV1CreateEndpointRequest
{
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
}
