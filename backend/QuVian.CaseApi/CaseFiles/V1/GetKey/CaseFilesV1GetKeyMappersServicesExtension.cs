using QuVian.CaseApi.CaseFiles.V1.GetKey.Command;
using QuVian.CaseApi.CaseFiles.V1.GetKey.Endpoint;
using QuVian.CaseApi.CaseFiles.V1.GetKey.Endpoint.Mappers;

namespace QuVian.CaseApi.CaseFiles.V1.GetKey;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CaseFilesV1GetKeyMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CaseFilesV1GetKeyCommandResult, CaseFilesV1GetKeyEndpointResponse>,
            CaseFilesV1GetKeyCommandResultToEndpointResponseMapper>();
        return services;
    }
}
