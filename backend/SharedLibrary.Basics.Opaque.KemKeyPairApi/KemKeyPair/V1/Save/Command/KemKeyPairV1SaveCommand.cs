namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Command;

public sealed record KemKeyPairV1SaveCommand
    : IHandleableCommand<KemKeyPairV1SaveCommand, KemKeyPairV1SaveCommandValidator, KemKeyPairV1SaveCommandHandler, KemKeyPairV1SaveCommandResult>
{
    public required string UserId { get; init; }
    public required string PublicKey { get; init; }
    public required string EncryptedPrivateKey { get; init; }
    public required string Nonce { get; init; }
}
