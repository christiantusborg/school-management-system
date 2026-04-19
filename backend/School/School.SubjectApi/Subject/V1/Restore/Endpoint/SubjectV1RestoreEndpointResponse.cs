namespace School.SubjectApi.Subject.V1.Restore.Endpoint;

public sealed class SubjectV1RestoreEndpointResponse : HateoasBaseResponse
{
    public required Guid SubjectId { get; init; }
}
