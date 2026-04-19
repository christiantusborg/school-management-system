using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Command;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Endpoint;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaSmsV1EnableConfirmMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaSmsV1EnableConfirmCommandResult, MfaSmsV1EnableConfirmEndpointResponse>,
            MfaSmsV1EnableConfirmCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<MfaSmsV1EnableConfirmEndpointRequest, MfaSmsV1EnableConfirmCommand>,
            MfaSmsV1EnableConfirmEndpointRequestToCommandMapper>();
        return services;
    }
}
