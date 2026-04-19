using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class PhonesV1VerifyConfirmMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PhonesV1VerifyConfirmCommandResult, PhonesV1VerifyConfirmEndpointResponse>,
            PhonesV1VerifyConfirmCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<PhonesV1VerifyConfirmEndpointRequest, PhonesV1VerifyConfirmCommand>,
            PhonesV1VerifyConfirmEndpointRequestToCommandMapper>();
        return services;
    }
}
