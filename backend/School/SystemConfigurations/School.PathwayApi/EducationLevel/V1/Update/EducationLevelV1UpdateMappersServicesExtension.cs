using School.PathwayApi.EducationLevel.V1.Update.Command;
using School.PathwayApi.EducationLevel.V1.Update.Endpoint;
using School.PathwayApi.EducationLevel.V1.Update.Endpoint.Mappers;

namespace School.PathwayApi.EducationLevel.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class EducationLevelV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EducationLevelV1UpdateEndpointRequest, EducationLevelV1UpdateCommand>,
            EducationLevelV1UpdateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<EducationLevelV1UpdateCommandResult, EducationLevelV1UpdateEndpointResponse>,
            EducationLevelV1UpdateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
