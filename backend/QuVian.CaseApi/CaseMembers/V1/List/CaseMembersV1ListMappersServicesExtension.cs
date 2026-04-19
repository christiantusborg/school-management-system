using QuVian.CaseApi.CaseMembers.V1.List.Command;
using QuVian.CaseApi.CaseMembers.V1.List.Endpoint;
using QuVian.CaseApi.CaseMembers.V1.List.Endpoint.Mappers;

namespace QuVian.CaseApi.CaseMembers.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CaseMembersV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CaseMembersV1ListCommandResult, CaseMembersV1ListEndpointResponse>,
            CaseMembersV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
