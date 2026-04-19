namespace School.SubjectApi.Subject.V1.PermanentDelete.Endpoint;

public sealed class SubjectV1PermanentDeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid SubjectId { get; init; }
}
