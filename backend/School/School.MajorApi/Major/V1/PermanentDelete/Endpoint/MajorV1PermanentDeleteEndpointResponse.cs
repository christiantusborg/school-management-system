namespace School.MajorApi.Major.V1.PermanentDelete.Endpoint;

public sealed class MajorV1PermanentDeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid MajorId { get; init; }
}
