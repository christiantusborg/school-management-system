using School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Endpoint;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Endpoint.Mappers;

public sealed class FinalProjectStatusV1UpdateEndpointRequestToCommandMapper
    : IMapper<FinalProjectStatusV1UpdateEndpointRequest, FinalProjectStatusV1UpdateCommand>
{
    public FinalProjectStatusV1UpdateCommand MapFrom(FinalProjectStatusV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new FinalProjectStatusV1UpdateCommand
        {
            FinalProjectStatusId = 0,
            Name = input.Name,
            Description = input.Description,
            AllowSetByPartner = input.AllowSetByPartner,
        };
    }
}
