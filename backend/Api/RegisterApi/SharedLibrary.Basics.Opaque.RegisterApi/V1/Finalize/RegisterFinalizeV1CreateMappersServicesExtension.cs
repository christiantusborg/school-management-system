using SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Command;
using SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Endpoint;
using SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize;

public class RegisterFinalizeV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<RegisterFinalizeV1CreateCommandResult, RegisterFinalizeV1CreateEndpointResponse>,
            RegisterFinalizeV1CreateCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<RegisterFinalizeV1CreateEndpointRequest, RegisterFinalizeV1CreateCommand>,
            RegisterFinalizeV1CreateEndpointRequestToCommandMapper>();
        return services;
    }
}
