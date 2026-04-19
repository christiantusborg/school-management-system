namespace School.MajorApi.Major.V1.Restore.Endpoint;

public sealed class MajorV1RestoreEndpointResponse : HateoasBaseResponse
{
    public required Guid MajorId { get; init; }
}
