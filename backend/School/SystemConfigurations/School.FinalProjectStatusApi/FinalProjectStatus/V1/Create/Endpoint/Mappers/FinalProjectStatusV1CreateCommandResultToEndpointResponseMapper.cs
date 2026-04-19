using School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Endpoint;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Endpoint.Mappers;

public sealed class FinalProjectStatusV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<FinalProjectStatusV1CreateCommandResult, FinalProjectStatusV1CreateEndpointResponse>
{
    public FinalProjectStatusV1CreateEndpointResponse MapFrom(FinalProjectStatusV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new FinalProjectStatusV1CreateEndpointResponse
        {
            FinalProjectStatusId = input.FinalProjectStatusId,
            Links = []
        };
    }
}
