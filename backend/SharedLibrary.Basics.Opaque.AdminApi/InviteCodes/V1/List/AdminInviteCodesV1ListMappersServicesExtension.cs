using SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Command;
using SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Endpoint;
using SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class AdminInviteCodesV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<AdminInviteCodesV1ListCommandResultItem>, AdminInviteCodesV1ListEndpointResponse>,
            AdminInviteCodesV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
