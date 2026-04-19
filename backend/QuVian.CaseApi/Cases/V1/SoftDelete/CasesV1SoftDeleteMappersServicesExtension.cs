using QuVian.CaseApi.Cases.V1.SoftDelete.Command;
using QuVian.CaseApi.Cases.V1.SoftDelete.Endpoint;
using QuVian.CaseApi.Cases.V1.SoftDelete.Endpoint.Mappers;

namespace QuVian.CaseApi.Cases.V1.SoftDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CasesV1SoftDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CasesV1SoftDeleteCommandResult, CasesV1SoftDeleteEndpointResponse>,
            CasesV1SoftDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
