using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Command;
using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Endpoint;
using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ChangePasswordV1FinalizeMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ChangePasswordV1FinalizeCommandResult, ChangePasswordV1FinalizeEndpointResponse>,
            ChangePasswordV1FinalizeCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<ChangePasswordV1FinalizeEndpointRequest, ChangePasswordV1FinalizeCommand>,
            ChangePasswordV1FinalizeEndpointRequestToCommandMapper>();
        return services;
    }
}
