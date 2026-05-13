using School.PathwayApi.EducationLevel.V1.Update.Command;

namespace School.PathwayApi.EducationLevel.V1.Update.Endpoint.Mappers;

public sealed class EducationLevelV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EducationLevelV1UpdateCommandResult, EducationLevelV1UpdateEndpointResponse>
{
    public EducationLevelV1UpdateEndpointResponse MapFrom(EducationLevelV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EducationLevelV1UpdateEndpointResponse
        {
            EducationLevelId = input.EducationLevelId,
            Links = []
        };
    }
}
