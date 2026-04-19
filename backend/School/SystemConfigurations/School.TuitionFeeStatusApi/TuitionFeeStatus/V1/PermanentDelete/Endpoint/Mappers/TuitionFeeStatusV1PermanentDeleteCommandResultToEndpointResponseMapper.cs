using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Endpoint;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Endpoint.Mappers;

public sealed class TuitionFeeStatusV1PermanentDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<TuitionFeeStatusV1PermanentDeleteCommandResult, TuitionFeeStatusV1PermanentDeleteEndpointResponse>
{
    public TuitionFeeStatusV1PermanentDeleteEndpointResponse MapFrom(TuitionFeeStatusV1PermanentDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new TuitionFeeStatusV1PermanentDeleteEndpointResponse
        {
            TuitionFeeStatusId = input.TuitionFeeStatusId,
            Links = []
        };
    }
}
