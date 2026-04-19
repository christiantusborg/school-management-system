namespace QuVian.CaseApi.CaseFiles.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CaseFilesV1ListCommandHandler(
    ICaseFileRepository fileRepository,
    ICaseUserMemberRepository memberRepository,
    ILogger<CaseFilesV1ListCommandHandler> logger)
    : ICommandHandler<CaseFilesV1ListCommand, CaseFilesV1ListCommandResult, CaseFile, ICaseFileRepository>
{
    public async Task<SuccessOrFailure<CaseFilesV1ListCommandResult>> HandleAsync(
        CaseFilesV1ListCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[CaseFiles:List] CaseId={CaseId} UserId={UserId}", command.CaseId, command.UserId);

        // Determine caller's access level on this case
        var memberSpec = new Specification<CaseUserMember>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.UserId == command.UserId);
        var member = await memberRepository.GetAsync(memberSpec, cancellationToken).ConfigureAwait(false);
        if (member is null)
            return SuccessOrFailureHelper<CaseFilesV1ListCommandResult>.Create("Not a member of this case.");

        int userLevel = member.Level;

        // Return files where the user's level qualifies: userLevel <= file.MinLevel
        var fileSpec = new Specification<CaseFile>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.DeletedAt == null)
            .AddWhere(x => x.MinLevel >= userLevel);

        var files = await fileRepository.SearchAsync(fileSpec, cancellationToken).ConfigureAwait(false);

        var items = files.Select(f => new CaseFileItem
        {
            CaseFileId      = f.CaseFileId,
            Name            = f.Name,
            ContentType     = f.ContentType,
            SizeBytes       = f.SizeBytes,
            MinLevel        = f.MinLevel,
            AccessMode      = f.AccessMode.ToString(),
            CreatedByUserId = f.CreatedByUserId,
            CreatedAt       = f.CreatedAt
        }).OrderByDescending(x => x.CreatedAt).ToList();

        return new SuccessOrFailure<CaseFilesV1ListCommandResult>(
            new CaseFilesV1ListCommandResult { Items = items });
    }
}
