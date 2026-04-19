using School.ProgrammeApi.Programme.V1.Restore.Command;
using School.ProgrammeApi.Programme.V1.Restore.Endpoint;

namespace School.ProgrammeApi.Programme.V1.Restore.Endpoint.Mappers;

public sealed class ProgrammeV1RestoreCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ProgrammeV1RestoreCommandResult, ProgrammeV1RestoreEndpointResponse>
{
    public ProgrammeV1RestoreEndpointResponse MapFrom(ProgrammeV1RestoreCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ProgrammeV1RestoreEndpointResponse
        {
            ProgrammeId = input.ProgrammeId,
            Links = []
        };
    }
}
