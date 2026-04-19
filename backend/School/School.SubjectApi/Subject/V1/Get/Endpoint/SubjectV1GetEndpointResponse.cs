namespace School.SubjectApi.Subject.V1.Get.Endpoint;

public sealed class SubjectV1GetEndpointResponse : HateoasBaseResponse
{
    public required Guid SubjectId { get; init; }
    public required Guid MajorId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int Ects { get; init; }
    public DateTime? DeletedAt { get; init; }
}
