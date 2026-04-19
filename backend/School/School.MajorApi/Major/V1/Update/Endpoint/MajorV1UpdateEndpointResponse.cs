namespace School.MajorApi.Major.V1.Update.Endpoint;

public sealed class MajorV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required Guid MajorId { get; init; }
}
