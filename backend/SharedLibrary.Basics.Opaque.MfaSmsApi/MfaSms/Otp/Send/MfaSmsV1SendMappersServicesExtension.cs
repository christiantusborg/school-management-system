using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Command;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Endpoint;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaSmsV1SendMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaSmsV1SendCommandResult, MfaSmsV1SendEndpointResponse>,
            MfaSmsV1SendCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<MfaSmsV1SendEndpointRequest, MfaSmsV1SendCommand>,
            MfaSmsV1SendEndpointRequestToCommandMapper>();
        return services;
    }
}
