using School.SubjectApi.Subject.V1.List.Command;
using School.SubjectApi.Subject.V1.List.Endpoint;

namespace School.SubjectApi.Subject.V1.List.Endpoint.Mappers;

public sealed class SubjectV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<SubjectV1ListCommandResultItem>, BaseGetAllResponse<SubjectV1ListEndpointResponseItem>>
{
    public BaseGetAllResponse<SubjectV1ListEndpointResponseItem> MapFrom(CommandSearchResult<SubjectV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new SubjectV1ListEndpointResponseItem
        {
            SubjectId = x.SubjectId,
            MajorId = x.MajorId,
            Code = x.Code,
            Name = x.Name,
            Ects = x.Ects,
            DeletedAt = x.DeletedAt,
            Links = []
        }).ToList();

        return new BaseGetAllResponse<SubjectV1ListEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
