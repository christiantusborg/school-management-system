using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Command;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Endpoint.Mappers;

public sealed class KemKeyPairV1SaveCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<KemKeyPairV1SaveCommandResult, KemKeyPairV1SaveEndpointResponse>
{
    public KemKeyPairV1SaveEndpointResponse MapFrom(KemKeyPairV1SaveCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new KemKeyPairV1SaveEndpointResponse
        {
            Links = []
        };
    }
}
