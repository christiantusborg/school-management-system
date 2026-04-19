using QuVian.CaseApi.Cases.V1.Update.Command;

namespace QuVian.CaseApi.Cases.V1.Update.Endpoint.Mappers;

public class CasesV1UpdateEndpointRequestToCommandMapper
    : IMapper<CasesV1UpdateEndpointRequest, CasesV1UpdateCommand>
{
    public CasesV1UpdateCommand MapFrom(CasesV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CasesV1UpdateCommand
        {
            CaseId = Guid.Empty, // overwritten in endpoint
            Name = input.Name,
            Description = input.Description,
            Status = input.Status,
            Priority = input.Priority,
            DueDate = input.DueDate
        };
    }
}
