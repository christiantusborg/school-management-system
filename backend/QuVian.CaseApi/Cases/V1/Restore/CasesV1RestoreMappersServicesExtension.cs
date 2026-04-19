using QuVian.CaseApi.Cases.V1.Restore.Command;
using QuVian.CaseApi.Cases.V1.Restore.Endpoint;
using QuVian.CaseApi.Cases.V1.Restore.Endpoint.Mappers;

namespace QuVian.CaseApi.Cases.V1.Restore;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CasesV1RestoreMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CasesV1RestoreCommandResult, CasesV1RestoreEndpointResponse>,
            CasesV1RestoreCommandResultToEndpointResponseMapper>();
        return services;
    }
}
