using QuVian.CaseApi.Cases.V1.Create.Command;
using QuVian.CaseApi.Cases.V1.Create.Endpoint;
using QuVian.CaseApi.Cases.V1.Create.Endpoint.Mappers;

namespace QuVian.CaseApi.Cases.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CasesV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CasesV1CreateEndpointRequest, CasesV1CreateCommand>,
            CasesV1CreateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<CasesV1CreateCommandResult, CasesV1CreateEndpointResponse>,
            CasesV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
