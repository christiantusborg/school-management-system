using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Command;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Endpoint;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaEmailV1EnableConfirmMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaEmailV1EnableConfirmCommandResult, MfaEmailV1EnableConfirmEndpointResponse>,
            MfaEmailV1EnableConfirmCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<MfaEmailV1EnableConfirmEndpointRequest, MfaEmailV1EnableConfirmCommand>,
            MfaEmailV1EnableConfirmEndpointRequestToCommandMapper>();
        return services;
    }
}
