using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Command;
using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Endpoint;
using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class PasswordResetV1FinalizeMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PasswordResetV1FinalizeCommandResult, PasswordResetV1FinalizeEndpointResponse>,
            PasswordResetV1FinalizeCommandResultToEndpointResponseMapper>();
        return services;
    }
}
