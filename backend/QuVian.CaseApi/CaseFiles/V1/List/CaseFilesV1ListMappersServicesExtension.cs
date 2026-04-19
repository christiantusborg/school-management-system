using QuVian.CaseApi.CaseFiles.V1.List.Command;
using QuVian.CaseApi.CaseFiles.V1.List.Endpoint;
using QuVian.CaseApi.CaseFiles.V1.List.Endpoint.Mappers;

namespace QuVian.CaseApi.CaseFiles.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CaseFilesV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CaseFilesV1ListCommandResult, CaseFilesV1ListEndpointResponse>,
            CaseFilesV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
