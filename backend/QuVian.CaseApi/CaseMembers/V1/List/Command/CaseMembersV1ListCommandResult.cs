namespace QuVian.CaseApi.CaseMembers.V1.List.Command;

public sealed class CaseMembersV1ListCommandResult : ICaseMembersV1ListCommandResultQueue
{
    public required List<CaseMemberItem> Users { get; init; }
    public required List<CaseTeamMemberItem> Teams { get; init; }
}

public class CaseMemberItem
{
    public required Guid CaseUserMemberId { get; init; }
    public required string UserId { get; init; }
    public int Level { get; init; }
    public string? Username { get; init; }
    public string? Email { get; init; }
    public required string GrantedByUserId { get; init; }
    public DateTime GrantedAt { get; init; }
}

public class CaseTeamMemberItem
{
    public required Guid CaseTeamMembershipId { get; init; }
    public required Guid TeamId { get; init; }
    public int Level { get; init; }
    public string? TeamName { get; init; }
    public required string GrantedByUserId { get; init; }
    public DateTime GrantedAt { get; init; }
}
