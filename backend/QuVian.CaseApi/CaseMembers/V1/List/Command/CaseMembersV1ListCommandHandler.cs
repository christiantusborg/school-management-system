namespace QuVian.CaseApi.CaseMembers.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CaseMembersV1ListCommandHandler(
    ICaseUserMemberRepository userMemberRepository,
    ICaseTeamMembershipRepository teamMembershipRepository,
    IApplicationUserRepository userRepository,
    ITeamRepository teamRepository,
    ILogger<CaseMembersV1ListCommandHandler> logger)
    : ICommandHandler<CaseMembersV1ListCommand, CaseMembersV1ListCommandResult, CaseUserMember, ICaseUserMemberRepository>
{
    public async Task<SuccessOrFailure<CaseMembersV1ListCommandResult>> HandleAsync(
        CaseMembersV1ListCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[CaseMembers:List] CaseId={CaseId}", command.CaseId);

        var userMemberSpec = new Specification<CaseUserMember>().AddWhere(x => x.CaseId == command.CaseId);
        var userMembers = await userMemberRepository.SearchAsync(userMemberSpec, cancellationToken).ConfigureAwait(false);

        var teamMemberSpec = new Specification<CaseTeamMembership>().AddWhere(x => x.CaseId == command.CaseId);
        var teamMemberships = await teamMembershipRepository.SearchAsync(teamMemberSpec, cancellationToken).ConfigureAwait(false);

        var userIds = userMembers.Select(m => m.UserId).ToHashSet();
        var users = (await userRepository.SearchAsync(new Specification<ApplicationUser>()
            .AddWhere(u => userIds.Contains(u.Id)), cancellationToken).ConfigureAwait(false))
            .ToDictionary(u => u.Id);

        var teamIds = teamMemberships.Select(t => t.TeamId).ToHashSet();
        var teams = (await teamRepository.SearchAsync(new Specification<Team>()
            .AddWhere(t => teamIds.Contains(t.TeamId)), cancellationToken).ConfigureAwait(false))
            .ToDictionary(t => t.TeamId);

        var userItems = userMembers.Select(m => new CaseMemberItem
        {
            CaseUserMemberId = m.CaseUserMemberId,
            UserId           = m.UserId,
            Level            = m.Level,
            Username         = users.TryGetValue(m.UserId, out var u) ? u.UserName : null,
            Email            = u?.Email,
            GrantedByUserId  = m.GrantedByUserId,
            GrantedAt        = m.GrantedAt
        }).ToList();

        var teamItems = teamMemberships.Select(m => new CaseTeamMemberItem
        {
            CaseTeamMembershipId = m.CaseTeamMembershipId,
            TeamId               = m.TeamId,
            Level                = m.Level,
            TeamName             = teams.TryGetValue(m.TeamId, out var t) ? t.Name : null,
            GrantedByUserId      = m.GrantedByUserId,
            GrantedAt            = m.GrantedAt
        }).ToList();

        return new SuccessOrFailure<CaseMembersV1ListCommandResult>(
            new CaseMembersV1ListCommandResult { Users = userItems, Teams = teamItems });
    }
}
