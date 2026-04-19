using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Endpoint.Mappers;

public sealed class KemKeyPairV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<KemKeyPairV1GetCommandResult, KemKeyPairV1GetEndpointResponse>
{
    public KemKeyPairV1GetEndpointResponse MapFrom(KemKeyPairV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new KemKeyPairV1GetEndpointResponse
        {
            PublicKey = input.PublicKey,
            EncryptedPrivateKey = input.EncryptedPrivateKey,
            Nonce = input.Nonce,
            Links = []
        };
    }
}
