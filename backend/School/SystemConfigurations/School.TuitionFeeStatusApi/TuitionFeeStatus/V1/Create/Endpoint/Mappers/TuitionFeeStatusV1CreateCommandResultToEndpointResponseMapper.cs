using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Endpoint;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Endpoint.Mappers;

public sealed class TuitionFeeStatusV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<TuitionFeeStatusV1CreateCommandResult, TuitionFeeStatusV1CreateEndpointResponse>
{
    public TuitionFeeStatusV1CreateEndpointResponse MapFrom(TuitionFeeStatusV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new TuitionFeeStatusV1CreateEndpointResponse
        {
            TuitionFeeStatusId = input.TuitionFeeStatusId,
            Links = []
        };
    }
}
