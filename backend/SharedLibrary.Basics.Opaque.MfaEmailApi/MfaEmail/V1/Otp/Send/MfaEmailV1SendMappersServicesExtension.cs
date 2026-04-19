using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Command;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Endpoint;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaEmailV1SendMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaEmailV1SendCommandResult, MfaEmailV1SendEndpointResponse>,
            MfaEmailV1SendCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<MfaEmailV1SendEndpointRequest, MfaEmailV1SendCommand>,
            MfaEmailV1SendEndpointRequestToCommandMapper>();
        return services;
    }
}
