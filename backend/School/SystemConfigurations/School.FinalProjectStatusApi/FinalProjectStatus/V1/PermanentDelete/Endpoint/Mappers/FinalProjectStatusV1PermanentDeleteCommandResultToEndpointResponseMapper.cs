using School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Endpoint;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Endpoint.Mappers;

public sealed class FinalProjectStatusV1PermanentDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<FinalProjectStatusV1PermanentDeleteCommandResult, FinalProjectStatusV1PermanentDeleteEndpointResponse>
{
    public FinalProjectStatusV1PermanentDeleteEndpointResponse MapFrom(FinalProjectStatusV1PermanentDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new FinalProjectStatusV1PermanentDeleteEndpointResponse
        {
            FinalProjectStatusId = input.FinalProjectStatusId,
            Links = []
        };
    }
}
