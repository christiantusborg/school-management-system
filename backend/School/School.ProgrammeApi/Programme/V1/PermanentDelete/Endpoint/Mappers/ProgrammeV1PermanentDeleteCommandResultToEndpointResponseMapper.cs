using School.ProgrammeApi.Programme.V1.PermanentDelete.Command;
using School.ProgrammeApi.Programme.V1.PermanentDelete.Endpoint;

namespace School.ProgrammeApi.Programme.V1.PermanentDelete.Endpoint.Mappers;

public sealed class ProgrammeV1PermanentDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ProgrammeV1PermanentDeleteCommandResult, ProgrammeV1PermanentDeleteEndpointResponse>
{
    public ProgrammeV1PermanentDeleteEndpointResponse MapFrom(ProgrammeV1PermanentDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ProgrammeV1PermanentDeleteEndpointResponse
        {
            ProgrammeId = input.ProgrammeId,
            Links = []
        };
    }
}
