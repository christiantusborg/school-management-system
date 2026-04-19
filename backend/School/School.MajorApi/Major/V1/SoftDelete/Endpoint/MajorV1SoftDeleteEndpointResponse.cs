namespace School.MajorApi.Major.V1.SoftDelete.Endpoint;

public sealed class MajorV1SoftDeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid MajorId { get; init; }
}
