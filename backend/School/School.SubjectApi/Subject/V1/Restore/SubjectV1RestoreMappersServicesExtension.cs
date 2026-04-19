using School.SubjectApi.Subject.V1.Restore.Command;
using School.SubjectApi.Subject.V1.Restore.Endpoint;
using School.SubjectApi.Subject.V1.Restore.Endpoint.Mappers;

namespace School.SubjectApi.Subject.V1.Restore;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class SubjectV1RestoreMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<SubjectV1RestoreCommandResult, SubjectV1RestoreEndpointResponse>,
            SubjectV1RestoreCommandResultToEndpointResponseMapper>();
        return services;
    }
}
