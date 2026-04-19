namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Command;

public sealed class KemKeyPairV1GetPublicKeyCommandResult : IKemKeyPairV1GetPublicKeyCommandResultQueue
{
    public required string PublicKey { get; init; }  // base64
}
