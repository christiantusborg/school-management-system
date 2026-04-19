using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm.Command;
using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm.Endpoint;
using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MfaTotpV1EnableConfirmMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MfaTotpV1EnableConfirmCommandResult, MfaTotpV1EnableConfirmEndpointResponse>,
            MfaTotpV1EnableConfirmCommandResultToEndpointResponseMapper>();
        return services;
    }
}
