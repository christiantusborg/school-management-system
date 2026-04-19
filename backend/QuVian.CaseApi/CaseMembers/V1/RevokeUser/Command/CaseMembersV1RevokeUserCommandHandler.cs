namespace QuVian.CaseApi.CaseMembers.V1.RevokeUser.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CaseMembersV1RevokeUserCommandHandler(
    ICaseUserMemberRepository memberRepository,
    ICaseUserKeyRepository userKeyRepository,
    ILogger<CaseMembersV1RevokeUserCommandHandler> logger)
    : ICommandHandler<CaseMembersV1RevokeUserCommand, CaseMembersV1RevokeUserCommandResult, CaseUserMember, ICaseUserMemberRepository>
{
    public async Task<SuccessOrFailure<CaseMembersV1RevokeUserCommandResult>> HandleAsync(
        CaseMembersV1RevokeUserCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[CaseMembers:RevokeUser] CaseId={CaseId} TargetUserId={TargetUserId} ActorUserId={ActorUserId}",
            command.CaseId, command.TargetUserId, command.ActorUserId);

        // Actor must be level 1 (Lead Partner) to revoke
        var actorSpec = new Specification<CaseUserMember>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.UserId == command.ActorUserId)
            .AddWhere(x => x.Level == 1);
        var actorMember = await memberRepository.GetAsync(actorSpec, cancellationToken).ConfigureAwait(false);
        if (actorMember is null)
        {
            logger.LogWarning("[CaseMembers:RevokeUser] FORBIDDEN — actor {ActorUserId} is not Lead Partner on case {CaseId}", command.ActorUserId, command.CaseId);
            return SuccessOrFailureHelper<CaseMembersV1RevokeUserCommandResult>.Create("Forbidden: only the Lead Partner can revoke access.");
        }

        var memberSpec = new Specification<CaseUserMember>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.UserId == command.TargetUserId);
        var member = await memberRepository.GetAsync(memberSpec, cancellationToken).ConfigureAwait(false);
        if (member is null)
            return SuccessOrFailureHelper<CaseMembersV1RevokeUserCommandResult>.EntityNotFound(typeof(CaseMembersV1RevokeUserCommand));

        memberRepository.Remove(member);

        // Remove all their level keys for this case
        var keySpec = new Specification<CaseUserKey>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.UserId == command.TargetUserId);
        var keys = await userKeyRepository.SearchAsync(keySpec, cancellationToken).ConfigureAwait(false);
        foreach (var k in keys) userKeyRepository.Remove(k);

        logger.LogInformation("[CaseMembers:RevokeUser] Revoked CaseUserMemberId={Id} and {KeyCount} keys", member.CaseUserMemberId, keys.Count);
        return new SuccessOrFailure<CaseMembersV1RevokeUserCommandResult>(
            new CaseMembersV1RevokeUserCommandResult { CaseUserMemberId = member.CaseUserMemberId });
    }
}
