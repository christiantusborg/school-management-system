using School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Endpoint;
using School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Endpoint.Mappers;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Restore;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ModeOfStudyV1RestoreMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ModeOfStudyV1RestoreCommandResult, ModeOfStudyV1RestoreEndpointResponse>,
            ModeOfStudyV1RestoreCommandResultToEndpointResponseMapper>();
        return services;
    }
}
