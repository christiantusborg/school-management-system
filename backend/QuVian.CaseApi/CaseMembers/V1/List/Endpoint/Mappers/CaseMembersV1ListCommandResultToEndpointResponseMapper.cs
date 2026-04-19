using QuVian.CaseApi.CaseMembers.V1.List.Command;

namespace QuVian.CaseApi.CaseMembers.V1.List.Endpoint.Mappers;

public class CaseMembersV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CaseMembersV1ListCommandResult, CaseMembersV1ListEndpointResponse>
{
    public CaseMembersV1ListEndpointResponse MapFrom(CaseMembersV1ListCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CaseMembersV1ListEndpointResponse { Users = input.Users, Teams = input.Teams, Links = [] };
    }
}
