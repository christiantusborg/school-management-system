namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class KemKeyPairV1GetPublicKeyCommandHandler(
    IKemKeyPairRepository repository,
    ILogger<KemKeyPairV1GetPublicKeyCommandHandler> logger)
    : ICommandHandler<KemKeyPairV1GetPublicKeyCommand, KemKeyPairV1GetPublicKeyCommandResult, Domains.KemKeyPair, IKemKeyPairRepository>
{
    public async Task<SuccessOrFailure<KemKeyPairV1GetPublicKeyCommandResult>> HandleAsync(
        KemKeyPairV1GetPublicKeyCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[KemKeyPair:GetPublicKey] UserId={UserId}", command.UserId);

        var spec = new Specification<Domains.KemKeyPair>()
            .AddWhere(x => x.UserId == command.UserId);

        var kemKeyPair = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (kemKeyPair is null)
        {
            logger.LogWarning("[KemKeyPair:GetPublicKey] NOT FOUND for UserId={UserId}", command.UserId);
            return SuccessOrFailureHelper<KemKeyPairV1GetPublicKeyCommandResult>.EntityNotFound(typeof(KemKeyPairV1GetPublicKeyCommand));
        }

        logger.LogInformation("[KemKeyPair:GetPublicKey] Found — PublicKeyBytes={Len}", kemKeyPair.PublicKey.Length);
        return new SuccessOrFailure<KemKeyPairV1GetPublicKeyCommandResult>(new KemKeyPairV1GetPublicKeyCommandResult
        {
            PublicKey = Convert.ToBase64String(kemKeyPair.PublicKey)
        });
    }
}
