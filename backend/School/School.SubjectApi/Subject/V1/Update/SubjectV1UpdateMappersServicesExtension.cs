using School.SubjectApi.Subject.V1.Update.Command;
using School.SubjectApi.Subject.V1.Update.Endpoint;
using School.SubjectApi.Subject.V1.Update.Endpoint.Mappers;

namespace School.SubjectApi.Subject.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class SubjectV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<SubjectV1UpdateEndpointRequest, SubjectV1UpdateCommand>,
            SubjectV1UpdateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<SubjectV1UpdateCommandResult, SubjectV1UpdateEndpointResponse>,
            SubjectV1UpdateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
