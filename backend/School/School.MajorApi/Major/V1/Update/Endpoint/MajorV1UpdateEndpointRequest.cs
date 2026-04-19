namespace School.MajorApi.Major.V1.Update.Endpoint;

public sealed class MajorV1UpdateEndpointRequest
{
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
}
