namespace QuVian.CaseApi.CaseUserKeys.V1.GetMy.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CaseUserKeysV1GetMyCommandHandler(
    ICaseUserKeyRepository repository,
    ILogger<CaseUserKeysV1GetMyCommandHandler> logger)
    : ICommandHandler<CaseUserKeysV1GetMyCommand, CaseUserKeysV1GetMyCommandResult, CaseUserKey, ICaseUserKeyRepository>
{
    public async Task<SuccessOrFailure<CaseUserKeysV1GetMyCommandResult>> HandleAsync(
        CaseUserKeysV1GetMyCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[CaseUserKeys:GetMy] CaseId={CaseId} UserId={UserId}", command.CaseId, command.UserId);

        var spec = new Specification<CaseUserKey>()
            .AddWhere(x => x.CaseId == command.CaseId)
            .AddWhere(x => x.UserId == command.UserId);

        var keys = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);

        var items = keys.Select(k => new CaseUserKeyItem
        {
            Level                = k.Level,
            KemCiphertext        = Convert.ToBase64String(k.KemCiphertext),
            EncryptedLevelPrivKey = Convert.ToBase64String(k.EncryptedLevelPrivKey),
            Nonce                = Convert.ToBase64String(k.Nonce)
        }).OrderBy(x => x.Level).ToList();

        return new SuccessOrFailure<CaseUserKeysV1GetMyCommandResult>(
            new CaseUserKeysV1GetMyCommandResult { Keys = items });
    }
}
