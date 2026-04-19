using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Command;
using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Endpoint;
using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaTotpV1VerifyMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaTotpV1VerifyEndpointRequest, MfaTotpV1VerifyCommand>,
            MfaTotpV1VerifyEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<MfaTotpV1VerifyCommandResult, MfaTotpV1VerifyEndpointResponse>,
            MfaTotpV1VerifyCommandResultToEndpointResponseMapper>();
        return services;
    }
}
