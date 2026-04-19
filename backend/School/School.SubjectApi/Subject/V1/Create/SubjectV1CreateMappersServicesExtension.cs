using School.SubjectApi.Subject.V1.Create.Command;
using School.SubjectApi.Subject.V1.Create.Endpoint;
using School.SubjectApi.Subject.V1.Create.Endpoint.Mappers;

namespace School.SubjectApi.Subject.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class SubjectV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<SubjectV1CreateEndpointRequest, SubjectV1CreateCommand>,
            SubjectV1CreateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<SubjectV1CreateCommandResult, SubjectV1CreateEndpointResponse>,
            SubjectV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
