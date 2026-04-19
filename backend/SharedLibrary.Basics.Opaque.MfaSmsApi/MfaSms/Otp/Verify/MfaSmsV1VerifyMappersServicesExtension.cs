using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Command;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Endpoint;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaSmsV1VerifyMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaSmsV1VerifyCommandResult, MfaSmsV1VerifyEndpointResponse>,
            MfaSmsV1VerifyCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<MfaSmsV1VerifyEndpointRequest, MfaSmsV1VerifyCommand>,
            MfaSmsV1VerifyEndpointRequestToCommandMapper>();
        return services;
    }
}
