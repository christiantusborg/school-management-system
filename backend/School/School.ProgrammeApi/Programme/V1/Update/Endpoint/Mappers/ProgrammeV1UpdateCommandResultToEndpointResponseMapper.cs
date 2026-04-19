using School.ProgrammeApi.Programme.V1.Update.Command;
using School.ProgrammeApi.Programme.V1.Update.Endpoint;

namespace School.ProgrammeApi.Programme.V1.Update.Endpoint.Mappers;

public sealed class ProgrammeV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ProgrammeV1UpdateCommandResult, ProgrammeV1UpdateEndpointResponse>
{
    public ProgrammeV1UpdateEndpointResponse MapFrom(ProgrammeV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ProgrammeV1UpdateEndpointResponse
        {
            ProgrammeId = input.ProgrammeId,
            Links = []
        };
    }
}
