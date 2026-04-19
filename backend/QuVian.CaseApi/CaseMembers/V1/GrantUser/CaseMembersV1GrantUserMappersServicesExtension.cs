using QuVian.CaseApi.CaseMembers.V1.GrantUser.Command;
using QuVian.CaseApi.CaseMembers.V1.GrantUser.Endpoint;
using QuVian.CaseApi.CaseMembers.V1.GrantUser.Endpoint.Mappers;

namespace QuVian.CaseApi.CaseMembers.V1.GrantUser;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CaseMembersV1GrantUserMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CaseMembersV1GrantUserCommandResult, CaseMembersV1GrantUserEndpointResponse>,
            CaseMembersV1GrantUserCommandResultToEndpointResponseMapper>();
        return services;
    }
}
