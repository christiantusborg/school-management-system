using School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Endpoint;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Endpoint.Mappers;

public sealed class FinalProjectStatusV1CreateEndpointRequestToCommandMapper
    : IMapper<FinalProjectStatusV1CreateEndpointRequest, FinalProjectStatusV1CreateCommand>
{
    public FinalProjectStatusV1CreateCommand MapFrom(FinalProjectStatusV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new FinalProjectStatusV1CreateCommand
        {
            Name = input.Name,
            Description = input.Description,
            AllowSetByPartner = input.AllowSetByPartner,
        };
    }
}
