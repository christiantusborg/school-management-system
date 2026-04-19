namespace QuVian.CaseApi.CaseMembers.V1.GrantUser.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CaseMembersV1GrantUserCommandHandler(
    ICaseUserMemberRepository memberRepository,
    ICaseUserKeyRepository userKeyRepository,
    ILogger<CaseMembersV1GrantUserCommandHandler> logger)
    : ICommandHandler<CaseMembersV1GrantUserCommand, CaseMembersV1GrantUserCommandResult, CaseUserMember, ICaseUserMemberRepository>
{
    public async Task<SuccessOrFailure<CaseMembersV1GrantUserCommandResult>> HandleAsync(
        CaseMembersV1GrantUserCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[CaseMembers:GrantUser] CaseId={CaseId} TargetUserId={TargetUserId} Level={Level} ActorUserId={ActorUserId}",
            command.CaseId, command.TargetUserId, command.Level, command.ActorUserId);

        // Security: actor must hold at least the requested level key
        var actorKeySpec = new Specification<CaseUserKey>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.UserId == command.ActorUserId)
            .AddWhere(x => x.Level == command.Level);

        var actorKey = await userKeyRepository.GetAsync(actorKeySpec, cancellationToken).ConfigureAwait(false);
        if (actorKey is null)
        {
            logger.LogWarning("[CaseMembers:GrantUser] FORBIDDEN — actor {ActorUserId} does not hold level {Level} key for case {CaseId}",
                command.ActorUserId, command.Level, command.CaseId);
            return SuccessOrFailureHelper<CaseMembersV1GrantUserCommandResult>.Create("Forbidden: you do not have permission to grant this access level.");
        }

        var member = new CaseUserMember
        {
            CaseId          = command.CaseId,
            UserId          = command.TargetUserId,
            Level           = command.Level,
            GrantedByUserId = command.ActorUserId,
            TenantId        = command.TenantId
        };
        memberRepository.Add(member);

        foreach (var wk in command.WrappedKeys)
        {
            userKeyRepository.Add(new CaseUserKey
            {
                CaseId               = command.CaseId,
                UserId               = command.TargetUserId,
                Level                = wk.Level,
                KemCiphertext        = wk.KemCiphertext,
                EncryptedLevelPrivKey = wk.EncryptedLevelPrivKey,
                Nonce                = wk.Nonce,
                TenantId             = command.TenantId
            });
        }

        logger.LogInformation("[CaseMembers:GrantUser] Granted CaseUserMemberId={Id}", member.CaseUserMemberId);
        return new SuccessOrFailure<CaseMembersV1GrantUserCommandResult>(
            new CaseMembersV1GrantUserCommandResult { CaseUserMemberId = member.CaseUserMemberId });
    }
}
