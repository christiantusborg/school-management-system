using QuVian.CaseApi.CaseUserKeys.V1.GetMy.Command;
using QuVian.CaseApi.CaseUserKeys.V1.GetMy.Endpoint;
using QuVian.CaseApi.CaseUserKeys.V1.GetMy.Endpoint.Mappers;

namespace QuVian.CaseApi.CaseUserKeys.V1.GetMy;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CaseUserKeysV1GetMyMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CaseUserKeysV1GetMyCommandResult, CaseUserKeysV1GetMyEndpointResponse>,
            CaseUserKeysV1GetMyCommandResultToEndpointResponseMapper>();
        return services;
    }
}
