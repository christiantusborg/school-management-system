using School.PathwayApi.Pathway.V1.List.Command;
using School.PathwayApi.Pathway.V1.List.Endpoint;

namespace School.PathwayApi.Pathway.V1.List.Endpoint.Mappers;

public sealed class PathwayV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<PathwayV1ListCommandResultItem>, BaseGetAllResponse<PathwayV1ListEndpointResponseItem>>
{
    public BaseGetAllResponse<PathwayV1ListEndpointResponseItem> MapFrom(CommandSearchResult<PathwayV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new PathwayV1ListEndpointResponseItem
        {
            PathwayId = x.PathwayId,
            Name = x.Name,

            Links = []
        }).ToList();

        return new BaseGetAllResponse<PathwayV1ListEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
