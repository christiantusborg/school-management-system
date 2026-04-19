using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Endpoint;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Endpoint.Mappers;

public sealed class TuitionFeeStatusV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<TuitionFeeStatusV1GetCommandResult, TuitionFeeStatusV1GetEndpointResponse>
{
    public TuitionFeeStatusV1GetEndpointResponse MapFrom(TuitionFeeStatusV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new TuitionFeeStatusV1GetEndpointResponse
        {
            TuitionFeeStatusId = input.TuitionFeeStatusId,
            Name = input.Name,

            DeletedAt = input.DeletedAt,
            Links = []
        };
    }
}
