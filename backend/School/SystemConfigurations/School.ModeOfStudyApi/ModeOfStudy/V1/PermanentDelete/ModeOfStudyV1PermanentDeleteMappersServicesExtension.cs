using School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Endpoint;
using School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Endpoint.Mappers;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ModeOfStudyV1PermanentDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ModeOfStudyV1PermanentDeleteCommandResult, ModeOfStudyV1PermanentDeleteEndpointResponse>,
            ModeOfStudyV1PermanentDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
