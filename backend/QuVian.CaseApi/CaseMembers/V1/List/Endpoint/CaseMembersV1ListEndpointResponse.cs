using QuVian.CaseApi.CaseMembers.V1.List.Command;

namespace QuVian.CaseApi.CaseMembers.V1.List.Endpoint;

public class CaseMembersV1ListEndpointResponse : HateoasBaseResponse
{
    public required List<CaseMemberItem> Users { get; init; }
    public required List<CaseTeamMemberItem> Teams { get; init; }
}
