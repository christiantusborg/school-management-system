using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Endpoint;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Endpoint.Mappers;

public sealed class TuitionFeeStatusV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<TuitionFeeStatusV1UpdateCommandResult, TuitionFeeStatusV1UpdateEndpointResponse>
{
    public TuitionFeeStatusV1UpdateEndpointResponse MapFrom(TuitionFeeStatusV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new TuitionFeeStatusV1UpdateEndpointResponse
        {
            TuitionFeeStatusId = input.TuitionFeeStatusId,
            Links = []
        };
    }
}
