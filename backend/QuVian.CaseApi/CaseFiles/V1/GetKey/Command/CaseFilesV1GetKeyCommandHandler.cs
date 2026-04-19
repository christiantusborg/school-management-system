namespace QuVian.CaseApi.CaseFiles.V1.GetKey.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CaseFilesV1GetKeyCommandHandler(
    ICaseFileRepository fileRepository,
    ICaseFileLevelKeyRepository levelKeyRepository,
    ICaseUserMemberRepository memberRepository,
    ILogger<CaseFilesV1GetKeyCommandHandler> logger)
    : ICommandHandler<CaseFilesV1GetKeyCommand, CaseFilesV1GetKeyCommandResult, CaseFile, ICaseFileRepository>
{
    public async Task<SuccessOrFailure<CaseFilesV1GetKeyCommandResult>> HandleAsync(
        CaseFilesV1GetKeyCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[CaseFiles:GetKey] CaseId={CaseId} FileId={FileId} UserId={UserId}",
            command.CaseId, command.CaseFileId, command.UserId);

        // Get the file
        var fileSpec = new Specification<CaseFile>()
            .AddWhere(x => x.CaseFileId == command.CaseFileId)
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.DeletedAt == null);
        var file = await fileRepository.GetAsync(fileSpec, cancellationToken).ConfigureAwait(false);
        if (file is null)
            return SuccessOrFailureHelper<CaseFilesV1GetKeyCommandResult>.EntityNotFound(typeof(CaseFilesV1GetKeyCommand));

        // Get caller's level
        var memberSpec = new Specification<CaseUserMember>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.UserId == command.UserId);
        var member = await memberRepository.GetAsync(memberSpec, cancellationToken).ConfigureAwait(false);
        if (member is null)
        {
            logger.LogWarning("[CaseFiles:GetKey] FORBIDDEN — {UserId} is not a case member", command.UserId);
            return SuccessOrFailureHelper<CaseFilesV1GetKeyCommandResult>.Create("Forbidden: not a case member.");
        }

        // Caller must have level <= file.MinLevel (Hierarchical) or == file.MinLevel (Independent)
        bool hasAccess = file.AccessMode == CaseFileAccessMode.Hierarchical
            ? member.Level <= file.MinLevel
            : member.Level == file.MinLevel;

        if (!hasAccess)
        {
            logger.LogWarning("[CaseFiles:GetKey] FORBIDDEN — level {UserLevel} cannot access file at minLevel {MinLevel} ({Mode})",
                member.Level, file.MinLevel, file.AccessMode);
            return SuccessOrFailureHelper<CaseFilesV1GetKeyCommandResult>.Create("Forbidden: insufficient access level.");
        }

        // Find the FileLevelKey row matching the user's level
        var keySpec = new Specification<CaseFileLevelKey>()
            .AddWhere(x => x.CaseFileId == command.CaseFileId)
            .AddWhere(x => x.Level == member.Level);
        var levelKey = await levelKeyRepository.GetAsync(keySpec, cancellationToken).ConfigureAwait(false);
        if (levelKey is null)
        {
            logger.LogError("[CaseFiles:GetKey] Missing FileLevelKey for FileId={FileId} Level={Level}", command.CaseFileId, member.Level);
            return SuccessOrFailureHelper<CaseFilesV1GetKeyCommandResult>.Create("File key not found for your access level.");
        }

        return new SuccessOrFailure<CaseFilesV1GetKeyCommandResult>(new CaseFilesV1GetKeyCommandResult
        {
            Level            = member.Level,
            KemCiphertext    = Convert.ToBase64String(levelKey.KemCiphertext),
            EncryptedFileKey = Convert.ToBase64String(levelKey.EncryptedFileKey),
            Nonce            = Convert.ToBase64String(levelKey.Nonce),
            StoragePath      = file.StoragePath
        });
    }
}
