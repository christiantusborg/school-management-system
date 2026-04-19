namespace School.ProgrammeApi.Programme.V1.Update.Endpoint;

public sealed class ProgrammeV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required Guid ProgrammeId { get; init; }
}
