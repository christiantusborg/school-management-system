using QuVian.CaseApi.CaseMembers.V1.GrantTeam.Command;
using QuVian.CaseApi.CaseMembers.V1.GrantTeam.Endpoint;
using QuVian.CaseApi.CaseMembers.V1.GrantTeam.Endpoint.Mappers;

namespace QuVian.CaseApi.CaseMembers.V1.GrantTeam;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CaseMembersV1GrantTeamMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CaseMembersV1GrantTeamCommandResult, CaseMembersV1GrantTeamEndpointResponse>,
            CaseMembersV1GrantTeamCommandResultToEndpointResponseMapper>();
        return services;
    }
}
