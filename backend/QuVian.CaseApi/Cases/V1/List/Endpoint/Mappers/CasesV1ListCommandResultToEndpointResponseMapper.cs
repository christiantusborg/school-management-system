using QuVian.CaseApi.Cases.V1.List.Command;

namespace QuVian.CaseApi.Cases.V1.List.Endpoint.Mappers;

public class CasesV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<CasesV1ListCommandResultItem>, BaseGetAllResponse<CasesV1ListEndpointResponseItem>>
{
    public BaseGetAllResponse<CasesV1ListEndpointResponseItem> MapFrom(CommandSearchResult<CasesV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new CasesV1ListEndpointResponseItem
        {
            CaseId = x.CaseId,
            Name = x.Name,
            Description = x.Description,
            Status = x.Status,
            Priority = x.Priority,
            DueDate = x.DueDate,
            CreatedByUserId = x.CreatedByUserId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, x.CaseId)
        }).ToList();

        return new BaseGetAllResponse<CasesV1ListEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
