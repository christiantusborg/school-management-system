using School.PathwayApi.EducationLevel.V1.SoftDelete.Command;

namespace School.PathwayApi.EducationLevel.V1.SoftDelete.Endpoint.Mappers;

public sealed class EducationLevelV1SoftDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EducationLevelV1SoftDeleteCommandResult, EducationLevelV1SoftDeleteEndpointResponse>
{
    public EducationLevelV1SoftDeleteEndpointResponse MapFrom(EducationLevelV1SoftDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EducationLevelV1SoftDeleteEndpointResponse
        {
            EducationLevelId = input.EducationLevelId,
            Links = []
        };
    }
}
