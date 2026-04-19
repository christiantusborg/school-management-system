using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class PhonesV1VerifyInitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PhonesV1VerifyInitCommandResult, PhonesV1VerifyInitEndpointResponse>,
            PhonesV1VerifyInitCommandResultToEndpointResponseMapper>();
        return services;
    }
}
