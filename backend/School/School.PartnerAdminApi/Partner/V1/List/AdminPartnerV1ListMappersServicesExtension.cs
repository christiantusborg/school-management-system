using School.PartnerAdminApi.Partner.V1.List.Command;
using School.PartnerAdminApi.Partner.V1.List.Endpoint;
using School.PartnerAdminApi.Partner.V1.List.Endpoint.Mappers;

namespace School.PartnerAdminApi.Partner.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class AdminPartnerV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<AdminPartnerV1ListCommandResultItem>, AdminPartnerV1ListEndpointResponse>,
            AdminPartnerV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
