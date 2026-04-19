using QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Command;
using QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Endpoint;
using QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Endpoint.Mappers;

namespace QuVian.CaseApi.CaseMembers.V1.RevokeTeam;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CaseMembersV1RevokeTeamMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CaseMembersV1RevokeTeamCommandResult, CaseMembersV1RevokeTeamEndpointResponse>,
            CaseMembersV1RevokeTeamCommandResultToEndpointResponseMapper>();
        return services;
    }
}
