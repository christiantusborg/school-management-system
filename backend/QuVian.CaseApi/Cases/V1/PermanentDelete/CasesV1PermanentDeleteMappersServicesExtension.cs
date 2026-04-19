using QuVian.CaseApi.Cases.V1.PermanentDelete.Command;
using QuVian.CaseApi.Cases.V1.PermanentDelete.Endpoint;
using QuVian.CaseApi.Cases.V1.PermanentDelete.Endpoint.Mappers;

namespace QuVian.CaseApi.Cases.V1.PermanentDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CasesV1PermanentDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CasesV1PermanentDeleteCommandResult, CasesV1PermanentDeleteEndpointResponse>,
            CasesV1PermanentDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
