using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Command;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Endpoint;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaEmailV1VerifyMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaEmailV1VerifyCommandResult, MfaEmailV1VerifyEndpointResponse>,
            MfaEmailV1VerifyCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<MfaEmailV1VerifyEndpointRequest, MfaEmailV1VerifyCommand>,
            MfaEmailV1VerifyEndpointRequestToCommandMapper>();
        return services;
    }
}
