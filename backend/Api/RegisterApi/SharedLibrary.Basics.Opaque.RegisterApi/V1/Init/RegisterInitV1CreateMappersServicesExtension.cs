using SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Command;
using SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Endpoint;
using SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Init;


public class RegisterInitV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<RegisterInitV1CreateCommandResult, RegisterInitV1CreateEndpointResponse>,
            RegisterInitV1CreateCommandResultToResponseMapper>();
        services.TryAddSingleton<IMapper<RegisterInitV1CreateEndpointRequest, RegisterInitV1CreateCommand>,
            RegisterInitV1CreateEndpointRequestToCommandMapper>();

        return services;
    }
}