using School.PathwayApi.EducationLevel.V1.Create.Command;
using School.PathwayApi.EducationLevel.V1.Create.Endpoint;
using School.PathwayApi.EducationLevel.V1.Create.Endpoint.Mappers;

namespace School.PathwayApi.EducationLevel.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class EducationLevelV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EducationLevelV1CreateEndpointRequest, EducationLevelV1CreateCommand>,
            EducationLevelV1CreateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<EducationLevelV1CreateCommandResult, EducationLevelV1CreateEndpointResponse>,
            EducationLevelV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
