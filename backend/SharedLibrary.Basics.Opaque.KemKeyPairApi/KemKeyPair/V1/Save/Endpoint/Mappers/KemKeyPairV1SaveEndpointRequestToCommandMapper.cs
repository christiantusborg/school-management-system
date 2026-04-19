using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Command;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Endpoint.Mappers;

public sealed class KemKeyPairV1SaveEndpointRequestToCommandMapper
    : IMapper<KemKeyPairV1SaveEndpointRequest, KemKeyPairV1SaveCommand>
{
    public KemKeyPairV1SaveCommand MapFrom(KemKeyPairV1SaveEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new KemKeyPairV1SaveCommand
        {
            UserId = string.Empty, // overwritten in endpoint
            PublicKey = input.PublicKey,
            EncryptedPrivateKey = input.EncryptedPrivateKey,
            Nonce = input.Nonce
        };
    }
}
