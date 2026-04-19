using QuVian.CaseApi.Cases.V1.Get.Command;

namespace QuVian.CaseApi.Cases.V1.Get.Endpoint.Mappers;

public class CasesV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CasesV1GetCommandResult, CasesV1GetEndpointResponse>
{
    public CasesV1GetEndpointResponse MapFrom(CasesV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CasesV1GetEndpointResponse
        {
            CaseId = input.CaseId,
            Name = input.Name,
            Description = input.Description,
            Status = input.Status,
            Priority = input.Priority,
            DueDate = input.DueDate,
            CreatedByUserId = input.CreatedByUserId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.CaseId)
        };
    }
}
