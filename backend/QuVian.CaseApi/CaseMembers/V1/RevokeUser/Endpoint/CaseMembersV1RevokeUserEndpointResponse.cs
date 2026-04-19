namespace QuVian.CaseApi.CaseMembers.V1.RevokeUser.Endpoint;

public class CaseMembersV1RevokeUserEndpointResponse : HateoasBaseResponse
{
    public required Guid CaseUserMemberId { get; init; }
}
