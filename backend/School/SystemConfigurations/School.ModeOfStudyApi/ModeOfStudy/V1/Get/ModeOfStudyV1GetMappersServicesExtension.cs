using School.ModeOfStudyApi.ModeOfStudy.V1.Get.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.Get.Endpoint;
using School.ModeOfStudyApi.ModeOfStudy.V1.Get.Endpoint.Mappers;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ModeOfStudyV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ModeOfStudyV1GetCommandResult, ModeOfStudyV1GetEndpointResponse>,
            ModeOfStudyV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}
