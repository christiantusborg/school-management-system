using School.ModeOfStudyApi.ModeOfStudy.V1.Update.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.Update.Endpoint;
using School.ModeOfStudyApi.ModeOfStudy.V1.Update.Endpoint.Mappers;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ModeOfStudyV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ModeOfStudyV1UpdateEndpointRequest, ModeOfStudyV1UpdateCommand>,
            ModeOfStudyV1UpdateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<ModeOfStudyV1UpdateCommandResult, ModeOfStudyV1UpdateEndpointResponse>,
            ModeOfStudyV1UpdateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
