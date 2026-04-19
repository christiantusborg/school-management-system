namespace QuVian.CaseApi.CaseMembers.V1.GrantTeam.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CaseMembersV1GrantTeamCommandHandler(
    ICaseTeamMembershipRepository membershipRepository,
    ICaseTeamKeyRepository teamKeyRepository,
    ICaseUserKeyRepository userKeyRepository,
    ILogger<CaseMembersV1GrantTeamCommandHandler> logger)
    : ICommandHandler<CaseMembersV1GrantTeamCommand, CaseMembersV1GrantTeamCommandResult, CaseTeamMembership, ICaseTeamMembershipRepository>
{
    public async Task<SuccessOrFailure<CaseMembersV1GrantTeamCommandResult>> HandleAsync(
        CaseMembersV1GrantTeamCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[CaseMembers:GrantTeam] CaseId={CaseId} TeamId={TeamId} Level={Level} ActorUserId={ActorUserId}",
            command.CaseId, command.TeamId, command.Level, command.ActorUserId);

        // Security: actor must hold the requested level key
        var actorKeySpec = new Specification<CaseUserKey>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.UserId == command.ActorUserId)
            .AddWhere(x => x.Level == command.Level);
        var actorKey = await userKeyRepository.GetAsync(actorKeySpec, cancellationToken).ConfigureAwait(false);
        if (actorKey is null)
        {
            logger.LogWarning("[CaseMembers:GrantTeam] FORBIDDEN — actor {ActorUserId} does not hold level {Level} key", command.ActorUserId, command.Level);
            return SuccessOrFailureHelper<CaseMembersV1GrantTeamCommandResult>.Create("Forbidden: you do not have permission to grant this access level.");
        }

        var membership = new CaseTeamMembership
        {
            CaseId          = command.CaseId,
            TeamId          = command.TeamId,
            Level           = command.Level,
            GrantedByUserId = command.ActorUserId,
            TenantId        = command.TenantId
        };
        membershipRepository.Add(membership);

        foreach (var wk in command.TeamWrappedKeys)
        {
            teamKeyRepository.Add(new CaseTeamKey
            {
                CaseId               = command.CaseId,
                TeamId               = command.TeamId,
                Level                = wk.Level,
                EncryptedLevelPrivKey = wk.EncryptedLevelPrivKey,
                Nonce                = wk.Nonce,
                TenantId             = command.TenantId
            });
        }

        logger.LogInformation("[CaseMembers:GrantTeam] Granted CaseTeamMembershipId={Id}", membership.CaseTeamMembershipId);
        return new SuccessOrFailure<CaseMembersV1GrantTeamCommandResult>(
            new CaseMembersV1GrantTeamCommandResult { CaseTeamMembershipId = membership.CaseTeamMembershipId });
    }
}
