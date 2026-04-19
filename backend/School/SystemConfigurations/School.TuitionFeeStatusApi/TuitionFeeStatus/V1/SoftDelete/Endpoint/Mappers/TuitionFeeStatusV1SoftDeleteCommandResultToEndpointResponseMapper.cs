using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Endpoint;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Endpoint.Mappers;

public sealed class TuitionFeeStatusV1SoftDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<TuitionFeeStatusV1SoftDeleteCommandResult, TuitionFeeStatusV1SoftDeleteEndpointResponse>
{
    public TuitionFeeStatusV1SoftDeleteEndpointResponse MapFrom(TuitionFeeStatusV1SoftDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new TuitionFeeStatusV1SoftDeleteEndpointResponse
        {
            TuitionFeeStatusId = input.TuitionFeeStatusId,
            Links = []
        };
    }
}
