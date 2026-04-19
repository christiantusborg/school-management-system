namespace School.ProgrammeApi.Programme.V1.Create.Endpoint;

public sealed class ProgrammeV1CreateEndpointResponse : HateoasBaseResponse
{
    public required Guid ProgrammeId { get; init; }
}
