using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class PhonesV1SetPrimaryMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PhonesV1SetPrimaryCommandResult, PhonesV1SetPrimaryEndpointResponse>,
            PhonesV1SetPrimaryCommandResultToEndpointResponseMapper>();
        return services;
    }
}
