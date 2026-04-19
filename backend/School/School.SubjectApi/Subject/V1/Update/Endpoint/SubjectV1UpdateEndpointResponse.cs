namespace School.SubjectApi.Subject.V1.Update.Endpoint;

public sealed class SubjectV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required Guid SubjectId { get; init; }
}
