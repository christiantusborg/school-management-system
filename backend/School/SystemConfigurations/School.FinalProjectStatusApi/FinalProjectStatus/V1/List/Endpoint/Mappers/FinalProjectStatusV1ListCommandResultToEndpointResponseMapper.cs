using School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Endpoint;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Endpoint.Mappers;

public sealed class FinalProjectStatusV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<FinalProjectStatusV1ListCommandResultItem>, BaseGetAllResponse<FinalProjectStatusV1ListEndpointResponseItem>>
{
    public BaseGetAllResponse<FinalProjectStatusV1ListEndpointResponseItem> MapFrom(CommandSearchResult<FinalProjectStatusV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new FinalProjectStatusV1ListEndpointResponseItem
        {
            FinalProjectStatusId = x.FinalProjectStatusId,
            Name = x.Name,
            Description = x.Description,
            AllowSetByPartner = x.AllowSetByPartner,
            Links = []
        }).ToList();

        return new BaseGetAllResponse<FinalProjectStatusV1ListEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
