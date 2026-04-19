namespace QuVian.CaseApi.CaseFiles.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CaseFilesV1CreateCommandHandler(
    ICaseFileRepository fileRepository,
    ICaseFileLevelKeyRepository levelKeyRepository,
    ICaseUserMemberRepository memberRepository,
    ILogger<CaseFilesV1CreateCommandHandler> logger)
    : ICommandHandler<CaseFilesV1CreateCommand, CaseFilesV1CreateCommandResult, CaseFile, ICaseFileRepository>
{
    public async Task<SuccessOrFailure<CaseFilesV1CreateCommandResult>> HandleAsync(
        CaseFilesV1CreateCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[CaseFiles:Create] CaseId={CaseId} Name={Name} MinLevel={MinLevel} AccessMode={Mode}",
            command.CaseId, command.Name, command.MinLevel, command.AccessMode);

        // Security: uploader must be a case member at or above MinLevel
        var memberSpec = new Specification<CaseUserMember>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.UserId == command.CreatedByUserId)
            .AddWhere(x => x.Level <= command.MinLevel);
        var member = await memberRepository.GetAsync(memberSpec, cancellationToken).ConfigureAwait(false);
        if (member is null)
        {
            logger.LogWarning("[CaseFiles:Create] FORBIDDEN — {UserId} cannot upload at level {Level}", command.CreatedByUserId, command.MinLevel);
            return SuccessOrFailureHelper<CaseFilesV1CreateCommandResult>.Create("Forbidden: you do not have access at this level.");
        }

        var file = new CaseFile
        {
            CaseId          = command.CaseId,
            Name            = command.Name,
            ContentType     = command.ContentType,
            SizeBytes       = command.SizeBytes,
            StoragePath     = command.StoragePath,
            MinLevel        = command.MinLevel,
            AccessMode      = command.AccessMode,
            CreatedByUserId = command.CreatedByUserId,
            TenantId        = command.TenantId
        };
        fileRepository.Add(file);
        logger.LogInformation("[CaseFiles:Create] CaseFileId={Id}", file.CaseFileId);

        foreach (var lk in command.LevelKeys)
        {
            levelKeyRepository.Add(new CaseFileLevelKey
            {
                CaseFileId      = file.CaseFileId,
                Level           = lk.Level,
                KemCiphertext   = lk.KemCiphertext,
                EncryptedFileKey = lk.EncryptedFileKey,
                Nonce           = lk.Nonce
            });
        }

        return new SuccessOrFailure<CaseFilesV1CreateCommandResult>(
            new CaseFilesV1CreateCommandResult { CaseFileId = file.CaseFileId });
    }
}
