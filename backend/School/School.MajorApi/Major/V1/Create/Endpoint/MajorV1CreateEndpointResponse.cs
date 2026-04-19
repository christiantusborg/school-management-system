namespace School.MajorApi.Major.V1.Create.Endpoint;

public sealed class MajorV1CreateEndpointResponse : HateoasBaseResponse
{
    public required Guid MajorId { get; init; }
}
