using School.MajorApi.Major.V1.List.Command;
using School.MajorApi.Major.V1.List.Endpoint;

namespace School.MajorApi.Major.V1.List.Endpoint.Mappers;

public sealed class MajorV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<MajorV1ListCommandResultItem>, BaseGetAllResponse<MajorV1ListEndpointResponseItem>>
{
    public BaseGetAllResponse<MajorV1ListEndpointResponseItem> MapFrom(CommandSearchResult<MajorV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new MajorV1ListEndpointResponseItem
        {
            MajorId = x.MajorId,
            ProgrammeId = x.ProgrammeId,
            Name = x.Name,
            DeletedAt = x.DeletedAt,
            Links = []
        }).ToList();

        return new BaseGetAllResponse<MajorV1ListEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
