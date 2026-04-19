using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class EmailsV1VerifyInitMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EmailsV1VerifyInitCommandResult, EmailsV1VerifyInitEndpointResponse>,
            EmailsV1VerifyInitCommandResultToEndpointResponseMapper>();
        return services;
    }
}
