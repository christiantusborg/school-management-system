namespace School.SubjectApi.Subject.V1.SoftDelete.Endpoint;

public sealed class SubjectV1SoftDeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid SubjectId { get; init; }
}
