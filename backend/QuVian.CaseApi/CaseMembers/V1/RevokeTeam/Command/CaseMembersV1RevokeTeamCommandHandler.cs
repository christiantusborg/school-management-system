namespace QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CaseMembersV1RevokeTeamCommandHandler(
    ICaseTeamMembershipRepository membershipRepository,
    ICaseTeamKeyRepository teamKeyRepository,
    ICaseUserMemberRepository userMemberRepository,
    ILogger<CaseMembersV1RevokeTeamCommandHandler> logger)
    : ICommandHandler<CaseMembersV1RevokeTeamCommand, CaseMembersV1RevokeTeamCommandResult, CaseTeamMembership, ICaseTeamMembershipRepository>
{
    public async Task<SuccessOrFailure<CaseMembersV1RevokeTeamCommandResult>> HandleAsync(
        CaseMembersV1RevokeTeamCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[CaseMembers:RevokeTeam] CaseId={CaseId} TeamId={TeamId}", command.CaseId, command.TeamId);

        var actorSpec = new Specification<CaseUserMember>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.UserId == command.ActorUserId)
            .AddWhere(x => x.Level == 1);
        var actorMember = await userMemberRepository.GetAsync(actorSpec, cancellationToken).ConfigureAwait(false);
        if (actorMember is null)
            return SuccessOrFailureHelper<CaseMembersV1RevokeTeamCommandResult>.Create("Forbidden: only the Lead Partner can revoke access.");

        var spec = new Specification<CaseTeamMembership>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.TeamId == command.TeamId);
        var membership = await membershipRepository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (membership is null)
            return SuccessOrFailureHelper<CaseMembersV1RevokeTeamCommandResult>.EntityNotFound(typeof(CaseMembersV1RevokeTeamCommand));

        membershipRepository.Remove(membership);

        var keySpec = new Specification<CaseTeamKey>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.TeamId == command.TeamId);
        var keys = await teamKeyRepository.SearchAsync(keySpec, cancellationToken).ConfigureAwait(false);
        foreach (var k in keys) teamKeyRepository.Remove(k);

        return new SuccessOrFailure<CaseMembersV1RevokeTeamCommandResult>(
            new CaseMembersV1RevokeTeamCommandResult { CaseTeamMembershipId = membership.CaseTeamMembershipId });
    }
}
