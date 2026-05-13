using School.PathwayApi.EducationLevel.V1.List.Command;

namespace School.PathwayApi.EducationLevel.V1.List.Endpoint.Mappers;

public sealed class EducationLevelV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<EducationLevelV1ListCommandResultItem>, BaseGetAllResponse<EducationLevelV1ListEndpointResponseItem>>
{
    public BaseGetAllResponse<EducationLevelV1ListEndpointResponseItem> MapFrom(CommandSearchResult<EducationLevelV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new EducationLevelV1ListEndpointResponseItem
        {
            EducationLevelId = x.EducationLevelId,
            Name = x.Name,
            Rank = x.Rank,
            DisplayOrder = x.DisplayOrder,
            Links = []
        }).ToList();

        return new BaseGetAllResponse<EducationLevelV1ListEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
