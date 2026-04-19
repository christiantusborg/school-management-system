using School.SubjectApi.Subject.V1.Get.Command;
using School.SubjectApi.Subject.V1.Get.Endpoint;
using School.SubjectApi.Subject.V1.Get.Endpoint.Mappers;

namespace School.SubjectApi.Subject.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class SubjectV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<SubjectV1GetCommandResult, SubjectV1GetEndpointResponse>,
            SubjectV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}
