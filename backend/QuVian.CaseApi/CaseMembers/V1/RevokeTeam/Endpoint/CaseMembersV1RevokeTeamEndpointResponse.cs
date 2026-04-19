namespace QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Endpoint;

public class CaseMembersV1RevokeTeamEndpointResponse : HateoasBaseResponse
{
    public required Guid CaseTeamMembershipId { get; init; }
}
