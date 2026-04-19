using QuVian.CaseApi.CaseMembers.V1.RevokeUser.Command;
using QuVian.CaseApi.CaseMembers.V1.RevokeUser.Endpoint;
using QuVian.CaseApi.CaseMembers.V1.RevokeUser.Endpoint.Mappers;

namespace QuVian.CaseApi.CaseMembers.V1.RevokeUser;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CaseMembersV1RevokeUserMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CaseMembersV1RevokeUserCommandResult, CaseMembersV1RevokeUserEndpointResponse>,
            CaseMembersV1RevokeUserCommandResultToEndpointResponseMapper>();
        return services;
    }
}
