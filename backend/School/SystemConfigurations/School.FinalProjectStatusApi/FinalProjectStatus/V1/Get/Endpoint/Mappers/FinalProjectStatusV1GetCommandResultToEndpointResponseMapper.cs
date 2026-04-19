using School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Endpoint;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Endpoint.Mappers;

public sealed class FinalProjectStatusV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<FinalProjectStatusV1GetCommandResult, FinalProjectStatusV1GetEndpointResponse>
{
    public FinalProjectStatusV1GetEndpointResponse MapFrom(FinalProjectStatusV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new FinalProjectStatusV1GetEndpointResponse
        {
            FinalProjectStatusId = input.FinalProjectStatusId,
            Name = input.Name,
            Description = input.Description,
            AllowSetByPartner = input.AllowSetByPartner,
            DeletedAt = input.DeletedAt,
            Links = []
        };
    }
}
