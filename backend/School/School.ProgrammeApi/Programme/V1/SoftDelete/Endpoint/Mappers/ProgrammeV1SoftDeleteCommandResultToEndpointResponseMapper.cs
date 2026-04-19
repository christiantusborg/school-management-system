using School.ProgrammeApi.Programme.V1.SoftDelete.Command;
using School.ProgrammeApi.Programme.V1.SoftDelete.Endpoint;

namespace School.ProgrammeApi.Programme.V1.SoftDelete.Endpoint.Mappers;

public sealed class ProgrammeV1SoftDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ProgrammeV1SoftDeleteCommandResult, ProgrammeV1SoftDeleteEndpointResponse>
{
    public ProgrammeV1SoftDeleteEndpointResponse MapFrom(ProgrammeV1SoftDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ProgrammeV1SoftDeleteEndpointResponse
        {
            ProgrammeId = input.ProgrammeId,
            Links = []
        };
    }
}
