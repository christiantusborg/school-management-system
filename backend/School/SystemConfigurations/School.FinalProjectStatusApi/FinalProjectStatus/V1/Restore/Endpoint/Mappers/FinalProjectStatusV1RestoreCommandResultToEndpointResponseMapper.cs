using School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Endpoint;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Endpoint.Mappers;

public sealed class FinalProjectStatusV1RestoreCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<FinalProjectStatusV1RestoreCommandResult, FinalProjectStatusV1RestoreEndpointResponse>
{
    public FinalProjectStatusV1RestoreEndpointResponse MapFrom(FinalProjectStatusV1RestoreCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new FinalProjectStatusV1RestoreEndpointResponse
        {
            FinalProjectStatusId = input.FinalProjectStatusId,
            Links = []
        };
    }
}
