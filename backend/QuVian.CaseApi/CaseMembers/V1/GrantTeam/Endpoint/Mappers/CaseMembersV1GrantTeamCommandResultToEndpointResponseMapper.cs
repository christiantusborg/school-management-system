using QuVian.CaseApi.CaseMembers.V1.GrantTeam.Command;

namespace QuVian.CaseApi.CaseMembers.V1.GrantTeam.Endpoint.Mappers;

public class CaseMembersV1GrantTeamCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CaseMembersV1GrantTeamCommandResult, CaseMembersV1GrantTeamEndpointResponse>
{
    public CaseMembersV1GrantTeamEndpointResponse MapFrom(CaseMembersV1GrantTeamCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CaseMembersV1GrantTeamEndpointResponse { CaseTeamMembershipId = input.CaseTeamMembershipId, Links = [] };
    }
}
