using School.MajorApi.Major.V1.Get.Command;
using School.MajorApi.Major.V1.Get.Endpoint;
using School.MajorApi.Major.V1.Get.Endpoint.Mappers;

namespace School.MajorApi.Major.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class MajorV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MajorV1GetCommandResult, MajorV1GetEndpointResponse>,
            MajorV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}
