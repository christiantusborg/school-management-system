using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Endpoint;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Endpoint.Mappers;

public sealed class TuitionFeeStatusV1UpdateEndpointRequestToCommandMapper
    : IMapper<TuitionFeeStatusV1UpdateEndpointRequest, TuitionFeeStatusV1UpdateCommand>
{
    public TuitionFeeStatusV1UpdateCommand MapFrom(TuitionFeeStatusV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new TuitionFeeStatusV1UpdateCommand
        {
            TuitionFeeStatusId = 0,
            Name = input.Name,

        };
    }
}
