using School.PathwayApi.EducationLevel.V1.Create.Command;

namespace School.PathwayApi.EducationLevel.V1.Create.Endpoint.Mappers;

public sealed class EducationLevelV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EducationLevelV1CreateCommandResult, EducationLevelV1CreateEndpointResponse>
{
    public EducationLevelV1CreateEndpointResponse MapFrom(EducationLevelV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EducationLevelV1CreateEndpointResponse
        {
            EducationLevelId = input.EducationLevelId,
            Links = []
        };
    }
}
