namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class KemKeyPairV1SaveCommandHandler(IKemKeyPairRepository repository)
    : ICommandHandler<KemKeyPairV1SaveCommand, KemKeyPairV1SaveCommandResult, Domains.KemKeyPair, IKemKeyPairRepository>
{
    public async Task<SuccessOrFailure<KemKeyPairV1SaveCommandResult>> HandleAsync(
        KemKeyPairV1SaveCommand command, CancellationToken cancellationToken)
    {
        byte[] publicKey;
        byte[] encryptedPrivateKey;
        byte[] nonce;

        try
        {
            publicKey = Convert.FromBase64String(command.PublicKey);
            encryptedPrivateKey = Convert.FromBase64String(command.EncryptedPrivateKey);
            nonce = Convert.FromBase64String(command.Nonce);
        }
        catch (FormatException)
        {
            return SuccessOrFailureHelper<KemKeyPairV1SaveCommandResult>.Create(
                $"{nameof(KemKeyPairV1SaveCommand)} - Invalid base64 encoding.");
        }

        var spec = new Specification<Domains.KemKeyPair>()
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            repository.Add(new Domains.KemKeyPair
            {
                UserId = command.UserId,
                PublicKey = publicKey,
                EncryptedPrivateKey = encryptedPrivateKey,
                Nonce = nonce
            });
        }
        else
        {
            existing.PublicKey = publicKey;
            existing.EncryptedPrivateKey = encryptedPrivateKey;
            existing.Nonce = nonce;
        }


        return new SuccessOrFailure<KemKeyPairV1SaveCommandResult>(new KemKeyPairV1SaveCommandResult());
    }
}
