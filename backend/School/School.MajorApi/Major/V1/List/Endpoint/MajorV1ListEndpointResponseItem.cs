namespace School.MajorApi.Major.V1.List.Endpoint;

public sealed class MajorV1ListEndpointResponseItem : HateoasBaseResponse
{
    public required Guid MajorId { get; init; }
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
    public DateTime? DeletedAt { get; init; }
}
