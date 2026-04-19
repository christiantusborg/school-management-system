using QuVian.CaseApi.Cases.V1.Update.Command;
using QuVian.CaseApi.Cases.V1.Update.Endpoint;
using QuVian.CaseApi.Cases.V1.Update.Endpoint.Mappers;

namespace QuVian.CaseApi.Cases.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CasesV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CasesV1UpdateEndpointRequest, CasesV1UpdateCommand>,
            CasesV1UpdateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<CasesV1UpdateCommandResult, CasesV1UpdateEndpointResponse>,
            CasesV1UpdateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
