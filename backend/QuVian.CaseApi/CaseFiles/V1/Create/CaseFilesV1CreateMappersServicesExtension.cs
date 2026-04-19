using QuVian.CaseApi.CaseFiles.V1.Create.Command;
using QuVian.CaseApi.CaseFiles.V1.Create.Endpoint;
using QuVian.CaseApi.CaseFiles.V1.Create.Endpoint.Mappers;

namespace QuVian.CaseApi.CaseFiles.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CaseFilesV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CaseFilesV1CreateCommandResult, CaseFilesV1CreateEndpointResponse>,
            CaseFilesV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
