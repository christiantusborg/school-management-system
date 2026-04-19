namespace QuVian.CaseApi.CaseMembers.V1.GrantUser.Endpoint;

public class CaseMembersV1GrantUserEndpointResponse : HateoasBaseResponse
{
    public required Guid CaseUserMemberId { get; init; }
}
