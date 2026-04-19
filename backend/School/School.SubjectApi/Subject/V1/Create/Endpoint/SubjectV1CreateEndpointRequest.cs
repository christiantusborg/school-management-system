namespace School.SubjectApi.Subject.V1.Create.Endpoint;

public sealed class SubjectV1CreateEndpointRequest
{
    public required Guid MajorId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int Ects { get; init; }
}
