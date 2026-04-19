namespace School.ProgrammeApi.Programme.V1.SoftDelete.Endpoint;

public sealed class ProgrammeV1SoftDeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid ProgrammeId { get; init; }
}
