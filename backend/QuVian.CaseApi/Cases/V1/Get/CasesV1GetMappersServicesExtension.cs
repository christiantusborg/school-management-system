using QuVian.CaseApi.Cases.V1.Get.Command;
using QuVian.CaseApi.Cases.V1.Get.Endpoint;
using QuVian.CaseApi.Cases.V1.Get.Endpoint.Mappers;

namespace QuVian.CaseApi.Cases.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CasesV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CasesV1GetCommandResult, CasesV1GetEndpointResponse>,
            CasesV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}
