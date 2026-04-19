namespace QuVian.CaseApi.CaseMembers.V1.GrantTeam.Endpoint;

public class CaseMembersV1GrantTeamEndpointResponse : HateoasBaseResponse
{
    public required Guid CaseTeamMembershipId { get; init; }
}
