namespace QuVian.CaseApi.Cases.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CasesV1CreateCommandHandler(
    ICaseRepository repository,
    ICaseKeyPairRepository keyPairRepository,
    ICaseUserKeyRepository userKeyRepository,
    ICaseUserMemberRepository userMemberRepository,
    ILogger<CasesV1CreateCommandHandler> logger)
    : ICommandHandler<CasesV1CreateCommand, CasesV1CreateCommandResult, Case, ICaseRepository>
{
    public async Task<SuccessOrFailure<CasesV1CreateCommandResult>> HandleAsync(
        CasesV1CreateCommand command, CancellationToken cancellationToken)
    {
        if (command.LevelKeyPairs.Count != 6)
            return SuccessOrFailureHelper<CasesV1CreateCommandResult>.Create("Exactly 6 level key pairs are required.");

        var entity = new Case
        {
            Name = command.Name,
            Description = command.Description,
            Priority = command.Priority,
            DueDate = command.DueDate,
            CreatedByUserId = command.CreatedByUserId,
            TenantId = command.TenantId
        };

        repository.Add(entity);
        logger.LogInformation("[Cases:Create] CaseId={CaseId} CreatedByUserId={UserId}", entity.CaseId, command.CreatedByUserId);

        // Store each level's public key and the creator's wrapped private key
        foreach (var lkp in command.LevelKeyPairs)
        {
            keyPairRepository.Add(new CaseKeyPair
            {
                CaseId    = entity.CaseId,
                Level     = lkp.Level,
                PublicKey = lkp.PublicKey,
                TenantId  = command.TenantId
            });

            userKeyRepository.Add(new CaseUserKey
            {
                CaseId               = entity.CaseId,
                UserId               = command.CreatedByUserId,
                Level                = lkp.Level,
                KemCiphertext        = lkp.KemCiphertext,
                EncryptedLevelPrivKey = lkp.EncryptedLevelPrivKey,
                Nonce                = lkp.Nonce,
                TenantId             = command.TenantId
            });
        }

        // Creator is Level 1 (Lead Partner) member
        userMemberRepository.Add(new CaseUserMember
        {
            CaseId          = entity.CaseId,
            UserId          = command.CreatedByUserId,
            Level           = 1,
            GrantedByUserId = command.CreatedByUserId,
            TenantId        = command.TenantId
        });

        return new SuccessOrFailure<CasesV1CreateCommandResult>(
            new CasesV1CreateCommandResult { CaseId = entity.CaseId });
    }
}
