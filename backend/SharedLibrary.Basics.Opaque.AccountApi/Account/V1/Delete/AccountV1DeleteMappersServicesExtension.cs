using SharedLibrary.Basics.Opaque.AccountApi.Account.V1.Delete.Command;
using SharedLibrary.Basics.Opaque.AccountApi.Account.V1.Delete.Endpoint;
using SharedLibrary.Basics.Opaque.AccountApi.Account.V1.Delete.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.AccountApi.Account.V1.Delete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class AccountV1DeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AccountV1DeleteCommandResult, AccountV1DeleteEndpointResponse>,
            AccountV1DeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
