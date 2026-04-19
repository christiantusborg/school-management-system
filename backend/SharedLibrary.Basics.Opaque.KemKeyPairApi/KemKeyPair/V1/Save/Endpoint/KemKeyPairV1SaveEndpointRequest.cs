namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Endpoint;

public sealed class KemKeyPairV1SaveEndpointRequest
{
    public required string PublicKey { get; init; }
    public required string EncryptedPrivateKey { get; init; }
    public required string Nonce { get; init; }
}
