using School.ModeOfStudyApi.ModeOfStudy.V1.List.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.List.Endpoint;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.List.Endpoint.Mappers;

public sealed class ModeOfStudyV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<ModeOfStudyV1ListCommandResultItem>, BaseGetAllResponse<ModeOfStudyV1ListEndpointResponseItem>>
{
    public BaseGetAllResponse<ModeOfStudyV1ListEndpointResponseItem> MapFrom(CommandSearchResult<ModeOfStudyV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new ModeOfStudyV1ListEndpointResponseItem
        {
            ModeOfStudyId = x.ModeOfStudyId,
            Name = x.Name,

            Links = []
        }).ToList();

        return new BaseGetAllResponse<ModeOfStudyV1ListEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
