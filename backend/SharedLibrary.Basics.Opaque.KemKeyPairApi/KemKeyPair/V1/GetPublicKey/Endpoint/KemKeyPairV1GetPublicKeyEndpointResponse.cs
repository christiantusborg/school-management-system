namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Endpoint;

public sealed class KemKeyPairV1GetPublicKeyEndpointResponse : HateoasBaseResponse
{
    public required string PublicKey { get; init; }  // base64
}
