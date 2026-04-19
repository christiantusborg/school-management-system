using School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Endpoint;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Endpoint.Mappers;

public sealed class FinalProjectStatusV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<FinalProjectStatusV1UpdateCommandResult, FinalProjectStatusV1UpdateEndpointResponse>
{
    public FinalProjectStatusV1UpdateEndpointResponse MapFrom(FinalProjectStatusV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new FinalProjectStatusV1UpdateEndpointResponse
        {
            FinalProjectStatusId = input.FinalProjectStatusId,
            Links = []
        };
    }
}
