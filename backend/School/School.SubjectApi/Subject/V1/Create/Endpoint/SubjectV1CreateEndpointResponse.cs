namespace School.SubjectApi.Subject.V1.Create.Endpoint;

public sealed class SubjectV1CreateEndpointResponse : HateoasBaseResponse
{
    public required Guid SubjectId { get; init; }
}
