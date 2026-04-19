using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Command;
using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Endpoint;
using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class LoginV1InitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<LoginV1InitEndpointRequest, LoginV1InitCommand>,
            LoginV1InitEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<LoginV1InitCommandResult, LoginV1InitEndpointResponse>,
            LoginV1InitCommandResultToEndpointResponseMapper>();
        return services;
    }
}
