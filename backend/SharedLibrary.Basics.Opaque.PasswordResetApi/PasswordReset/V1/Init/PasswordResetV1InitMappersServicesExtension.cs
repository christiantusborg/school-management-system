using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Command;
using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Endpoint;
using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class PasswordResetV1InitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PasswordResetV1InitCommandResult, PasswordResetV1InitEndpointResponse>,
            PasswordResetV1InitCommandResultToEndpointResponseMapper>();
        return services;
    }
}
