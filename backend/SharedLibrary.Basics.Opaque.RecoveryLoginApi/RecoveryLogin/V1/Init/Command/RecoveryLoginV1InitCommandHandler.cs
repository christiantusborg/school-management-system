namespace SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RecoveryLoginV1InitCommandHandler(
    UserManager<ApplicationUser> userManager,
    IOpaqueRecoveryCodeRepository recoveryCodeRepository,
    OpaqueServer opaqueServer,
    ITransientStateCache transientStateCache,
    IGuidProvider guidProvider)
    : ICommandHandler<RecoveryLoginV1InitCommand, RecoveryLoginV1InitCommandResult,
        OpaqueRecoveryCode, IOpaqueRecoveryCodeRepository>
{
    public async Task<SuccessOrFailure<RecoveryLoginV1InitCommandResult>> HandleAsync(
        RecoveryLoginV1InitCommand command, CancellationToken cancellationToken)
    {
        byte[] blindedElement;
        try
        {
            blindedElement = Convert.FromBase64String(command.BlindedElement);
            if (blindedElement.Length != 32)
                return TimingSafeFakeResponse();
        }
        catch
        {
            return TimingSafeFakeResponse();
        }

        var user = await userManager.FindByNameAsync(command.Username);
        if (user is null || !user.IsEnabled)
            return TimingSafeFakeResponse();

        var spec = new Specification<OpaqueRecoveryCode>()
            .AddWhere(x => x.UserId == user.Id)
            .AddWhere(x => x.CodeId == command.CodeId);

        var row = await recoveryCodeRepository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (row is null)
            return TimingSafeFakeResponse();

        var evaluatedElement = opaqueServer.BlindEvaluate(row.OprfSeed, blindedElement);
        var challenge = opaqueServer.GenerateChallenge();

        var loginId = guidProvider.NewId().ToString("N");
        var state = new OpaqueLoginInitState
        {
            UserId = user.Id,
            Challenge = challenge,
            ClientPublicKey = row.ClientPublicKey,
            DeviceInfo = command.DeviceInfo,
            OpaqueRecoveryCodeId = row.OpaqueRecoveryCodeId
        };

        await transientStateCache.SetAsync($"login:{loginId}", state, TimeSpan.FromMinutes(2));

        return new SuccessOrFailure<RecoveryLoginV1InitCommandResult>(new RecoveryLoginV1InitCommandResult
        {
            LoginId = loginId,
            EvaluatedElement = Convert.ToBase64String(evaluatedElement),
            Challenge = Convert.ToBase64String(challenge),
            EncryptedPrivateKey = Convert.ToBase64String(row.EncryptedPrivateKey),
            Nonce = Convert.ToBase64String(row.Nonce)
        });
    }

    private SuccessOrFailure<RecoveryLoginV1InitCommandResult> TimingSafeFakeResponse()
    {
        var fakeEvaluated = opaqueServer.GenerateChallenge();
        var fakeChallenge = opaqueServer.GenerateChallenge();
        var fakeLoginId = guidProvider.NewId().ToString("N");
        var fakeEncKey = opaqueServer.GenerateChallenge();
        var fakeNonce = new byte[12];
        RandomNumberGenerator.Fill(fakeNonce);

        return new SuccessOrFailure<RecoveryLoginV1InitCommandResult>(new RecoveryLoginV1InitCommandResult
        {
            LoginId = fakeLoginId,
            EvaluatedElement = Convert.ToBase64String(fakeEvaluated),
            Challenge = Convert.ToBase64String(fakeChallenge),
            EncryptedPrivateKey = Convert.ToBase64String(fakeEncKey),
            Nonce = Convert.ToBase64String(fakeNonce)
        });
    }
}
