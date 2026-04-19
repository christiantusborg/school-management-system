namespace School.ProgrammeApi.Programme.V1.PermanentDelete.Endpoint;

public sealed class ProgrammeV1PermanentDeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid ProgrammeId { get; init; }
}
