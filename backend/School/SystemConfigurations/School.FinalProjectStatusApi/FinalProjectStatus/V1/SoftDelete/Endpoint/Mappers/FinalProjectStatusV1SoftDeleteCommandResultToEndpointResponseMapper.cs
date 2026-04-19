using School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Endpoint;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Endpoint.Mappers;

public sealed class FinalProjectStatusV1SoftDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<FinalProjectStatusV1SoftDeleteCommandResult, FinalProjectStatusV1SoftDeleteEndpointResponse>
{
    public FinalProjectStatusV1SoftDeleteEndpointResponse MapFrom(FinalProjectStatusV1SoftDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new FinalProjectStatusV1SoftDeleteEndpointResponse
        {
            FinalProjectStatusId = input.FinalProjectStatusId,
            Links = []
        };
    }
}
