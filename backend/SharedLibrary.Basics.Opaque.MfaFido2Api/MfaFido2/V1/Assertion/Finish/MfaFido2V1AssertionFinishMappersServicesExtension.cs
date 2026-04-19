using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Command;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Endpoint;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaFido2V1AssertionFinishMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaFido2V1AssertionFinishCommandResult, MfaFido2V1AssertionFinishEndpointResponse>,
            MfaFido2V1AssertionFinishCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<MfaFido2V1AssertionFinishEndpointRequest, MfaFido2V1AssertionFinishCommand>,
            MfaFido2V1AssertionFinishEndpointRequestToCommandMapper>();
        return services;
    }
}
