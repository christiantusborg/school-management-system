namespace School.ProgrammeApi.Programme.V1.Restore.Endpoint;

public sealed class ProgrammeV1RestoreEndpointResponse : HateoasBaseResponse
{
    public required Guid ProgrammeId { get; init; }
}
