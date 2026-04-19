using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Endpoint;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Endpoint.Mappers;

public sealed class TuitionFeeStatusV1CreateEndpointRequestToCommandMapper
    : IMapper<TuitionFeeStatusV1CreateEndpointRequest, TuitionFeeStatusV1CreateCommand>
{
    public TuitionFeeStatusV1CreateCommand MapFrom(TuitionFeeStatusV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new TuitionFeeStatusV1CreateCommand
        {
            Name = input.Name,

        };
    }
}
