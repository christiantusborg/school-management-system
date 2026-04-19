using School.DocumentTypeApi.DocumentType.V1.List.Command;
using School.DocumentTypeApi.DocumentType.V1.List.Endpoint;

namespace School.DocumentTypeApi.DocumentType.V1.List.Endpoint.Mappers;

public sealed class DocumentTypeV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<DocumentTypeV1ListCommandResultItem>, BaseGetAllResponse<DocumentTypeV1ListEndpointResponseItem>>
{
    public BaseGetAllResponse<DocumentTypeV1ListEndpointResponseItem> MapFrom(CommandSearchResult<DocumentTypeV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new DocumentTypeV1ListEndpointResponseItem
        {
            DocumentTypeId = x.DocumentTypeId,
            Name = x.Name,
            Description = x.Description,
            Links = []
        }).ToList();

        return new BaseGetAllResponse<DocumentTypeV1ListEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
