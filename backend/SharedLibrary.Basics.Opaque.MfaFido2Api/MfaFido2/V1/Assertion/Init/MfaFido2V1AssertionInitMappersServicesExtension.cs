using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Command;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Endpoint;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaFido2V1AssertionInitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaFido2V1AssertionInitCommandResult, MfaFido2V1AssertionInitEndpointResponse>,
            MfaFido2V1AssertionInitCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<MfaFido2V1AssertionInitEndpointRequest, MfaFido2V1AssertionInitCommand>,
            MfaFido2V1AssertionInitEndpointRequestToCommandMapper>();
        return services;
    }
}
