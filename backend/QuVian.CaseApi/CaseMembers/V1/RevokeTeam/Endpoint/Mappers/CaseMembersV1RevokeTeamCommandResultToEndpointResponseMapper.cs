using QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Command;

namespace QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Endpoint.Mappers;

public class CaseMembersV1RevokeTeamCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CaseMembersV1RevokeTeamCommandResult, CaseMembersV1RevokeTeamEndpointResponse>
{
    public CaseMembersV1RevokeTeamEndpointResponse MapFrom(CaseMembersV1RevokeTeamCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CaseMembersV1RevokeTeamEndpointResponse { CaseTeamMembershipId = input.CaseTeamMembershipId, Links = [] };
    }
}
