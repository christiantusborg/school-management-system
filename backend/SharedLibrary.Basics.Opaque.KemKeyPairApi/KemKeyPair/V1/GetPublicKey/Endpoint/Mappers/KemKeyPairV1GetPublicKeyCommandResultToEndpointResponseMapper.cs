using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Command;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Endpoint.Mappers;

public sealed class KemKeyPairV1GetPublicKeyCommandResultToEndpointResponseMapper
    : IMapper<KemKeyPairV1GetPublicKeyCommandResult, KemKeyPairV1GetPublicKeyEndpointResponse>
{
    public KemKeyPairV1GetPublicKeyEndpointResponse MapFrom(KemKeyPairV1GetPublicKeyCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new KemKeyPairV1GetPublicKeyEndpointResponse
        {
            PublicKey = input.PublicKey,
            Links = []
        };
    }
}
