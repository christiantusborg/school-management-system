using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Command;
using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Endpoint;
using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ChangePasswordV1InitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ChangePasswordV1InitCommandResult, ChangePasswordV1InitEndpointResponse>,
            ChangePasswordV1InitCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<ChangePasswordV1InitEndpointRequest, ChangePasswordV1InitCommand>,
            ChangePasswordV1InitEndpointRequestToCommandMapper>();
        return services;
    }
}
