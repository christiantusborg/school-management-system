using QuVian.CaseApi.Cases.V1.List.Command;
using QuVian.CaseApi.Cases.V1.List.Endpoint;
using QuVian.CaseApi.Cases.V1.List.Endpoint.Mappers;

namespace QuVian.CaseApi.Cases.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CasesV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CommandSearchResult<CasesV1ListCommandResultItem>, BaseGetAllResponse<CasesV1ListEndpointResponseItem>>,
            CasesV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
