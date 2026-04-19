namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class KemKeyPairV1GetCommandHandler(
    IKemKeyPairRepository repository,
    ILogger<KemKeyPairV1GetCommandHandler> logger)
    : ICommandHandler<KemKeyPairV1GetCommand, KemKeyPairV1GetCommandResult, Domains.KemKeyPair, IKemKeyPairRepository>
{
    public async Task<SuccessOrFailure<KemKeyPairV1GetCommandResult>> HandleAsync(
        KemKeyPairV1GetCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[KemKeyPair:Get] UserId={UserId}", command.UserId);

        var spec = new Specification<Domains.KemKeyPair>()
            .AddWhere(x => x.UserId == command.UserId);

        var kemKeyPair = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (kemKeyPair is null)
        {
            logger.LogWarning("[KemKeyPair:Get] NOT FOUND for UserId={UserId}", command.UserId);
            return SuccessOrFailureHelper<KemKeyPairV1GetCommandResult>.EntityNotFound(typeof(KemKeyPairV1GetCommand));
        }

        logger.LogInformation("[KemKeyPair:Get] Found — PublicKeyBytes={Pub} EncryptedPrivKeyBytes={Priv} NonceBytes={Nonce}",
            kemKeyPair.PublicKey.Length, kemKeyPair.EncryptedPrivateKey.Length, kemKeyPair.Nonce.Length);

        return new SuccessOrFailure<KemKeyPairV1GetCommandResult>(new KemKeyPairV1GetCommandResult
        {
            PublicKey           = Convert.ToBase64String(kemKeyPair.PublicKey),
            EncryptedPrivateKey = Convert.ToBase64String(kemKeyPair.EncryptedPrivateKey),
            Nonce               = Convert.ToBase64String(kemKeyPair.Nonce)
        });
    }
}
