using School.ProgrammeApi.Programme.V1.Create.Command;
using School.ProgrammeApi.Programme.V1.Create.Endpoint;

namespace School.ProgrammeApi.Programme.V1.Create.Endpoint.Mappers;

public sealed class ProgrammeV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ProgrammeV1CreateCommandResult, ProgrammeV1CreateEndpointResponse>
{
    public ProgrammeV1CreateEndpointResponse MapFrom(ProgrammeV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ProgrammeV1CreateEndpointResponse
        {
            ProgrammeId = input.ProgrammeId,
            Links = []
        };
    }
}
