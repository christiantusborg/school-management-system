using School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Endpoint;
using School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Endpoint.Mappers;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ModeOfStudyV1SoftDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ModeOfStudyV1SoftDeleteCommandResult, ModeOfStudyV1SoftDeleteEndpointResponse>,
            ModeOfStudyV1SoftDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
