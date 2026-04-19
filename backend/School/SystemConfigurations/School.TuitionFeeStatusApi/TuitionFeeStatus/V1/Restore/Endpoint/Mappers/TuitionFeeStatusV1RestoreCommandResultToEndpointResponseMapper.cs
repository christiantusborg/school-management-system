using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Endpoint;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Endpoint.Mappers;

public sealed class TuitionFeeStatusV1RestoreCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<TuitionFeeStatusV1RestoreCommandResult, TuitionFeeStatusV1RestoreEndpointResponse>
{
    public TuitionFeeStatusV1RestoreEndpointResponse MapFrom(TuitionFeeStatusV1RestoreCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new TuitionFeeStatusV1RestoreEndpointResponse
        {
            TuitionFeeStatusId = input.TuitionFeeStatusId,
            Links = []
        };
    }
}
