using School.ModeOfStudyApi.ModeOfStudy.V1.Create.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.Create.Endpoint;
using School.ModeOfStudyApi.ModeOfStudy.V1.Create.Endpoint.Mappers;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ModeOfStudyV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ModeOfStudyV1CreateEndpointRequest, ModeOfStudyV1CreateCommand>,
            ModeOfStudyV1CreateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<ModeOfStudyV1CreateCommandResult, ModeOfStudyV1CreateEndpointResponse>,
            ModeOfStudyV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
