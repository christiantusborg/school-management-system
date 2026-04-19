using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Command;
using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Endpoint;
using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class LoginV1FinishMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<LoginV1FinishEndpointRequest, LoginV1FinishCommand>,
            LoginV1FinishEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<LoginV1FinishCommandResult, LoginV1FinishEndpointResponse>,
            LoginV1FinishCommandResultToEndpointResponseMapper>();
        return services;
    }
}
