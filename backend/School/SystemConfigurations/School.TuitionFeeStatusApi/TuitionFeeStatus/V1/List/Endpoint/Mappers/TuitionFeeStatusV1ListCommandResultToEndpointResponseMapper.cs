using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Endpoint;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Endpoint.Mappers;

public sealed class TuitionFeeStatusV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<TuitionFeeStatusV1ListCommandResultItem>, BaseGetAllResponse<TuitionFeeStatusV1ListEndpointResponseItem>>
{
    public BaseGetAllResponse<TuitionFeeStatusV1ListEndpointResponseItem> MapFrom(CommandSearchResult<TuitionFeeStatusV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new TuitionFeeStatusV1ListEndpointResponseItem
        {
            TuitionFeeStatusId = x.TuitionFeeStatusId,
            Name = x.Name,

            Links = []
        }).ToList();

        return new BaseGetAllResponse<TuitionFeeStatusV1ListEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
