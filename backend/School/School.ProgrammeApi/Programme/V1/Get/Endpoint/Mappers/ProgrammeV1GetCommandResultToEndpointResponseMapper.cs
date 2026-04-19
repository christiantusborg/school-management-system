using School.ProgrammeApi.Programme.V1.Get.Command;
using School.ProgrammeApi.Programme.V1.Get.Endpoint;

namespace School.ProgrammeApi.Programme.V1.Get.Endpoint.Mappers;

public sealed class ProgrammeV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ProgrammeV1GetCommandResult, ProgrammeV1GetEndpointResponse>
{
    public ProgrammeV1GetEndpointResponse MapFrom(ProgrammeV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ProgrammeV1GetEndpointResponse
        {
            ProgrammeId = input.ProgrammeId,
            Name = input.Name,
            Code = input.Code,
            PathwayIds = input.PathwayIds,
            DeletedAt = input.DeletedAt,
            Links = []
        };
    }
}
