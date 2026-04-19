namespace School.MajorApi.Major.V1.Get.Endpoint;

public sealed class MajorV1GetEndpointResponse : HateoasBaseResponse
{
    public required Guid MajorId { get; init; }
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
    public DateTime? DeletedAt { get; init; }
}
