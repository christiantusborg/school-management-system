using SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Command;
using SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Endpoint;
using SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class AdminInviteCodesV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AdminInviteCodesV1CreateCommandResult, AdminInviteCodesV1CreateEndpointResponse>,
            AdminInviteCodesV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
