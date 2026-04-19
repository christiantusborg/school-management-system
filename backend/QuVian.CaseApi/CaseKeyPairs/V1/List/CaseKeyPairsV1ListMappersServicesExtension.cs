using QuVian.CaseApi.CaseKeyPairs.V1.List.Command;
using QuVian.CaseApi.CaseKeyPairs.V1.List.Endpoint;
using QuVian.CaseApi.CaseKeyPairs.V1.List.Endpoint.Mappers;

namespace QuVian.CaseApi.CaseKeyPairs.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CaseKeyPairsV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CaseKeyPairsV1ListCommandResult, CaseKeyPairsV1ListEndpointResponse>,
            CaseKeyPairsV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
