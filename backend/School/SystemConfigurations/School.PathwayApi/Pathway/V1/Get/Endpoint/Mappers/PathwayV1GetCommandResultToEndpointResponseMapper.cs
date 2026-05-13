using School.PathwayApi.Pathway.V1.Get.Command;

namespace School.PathwayApi.Pathway.V1.Get.Endpoint.Mappers;

public sealed class PathwayV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PathwayV1GetCommandResult, PathwayV1GetEndpointResponse>
{
    public PathwayV1GetEndpointResponse MapFrom(PathwayV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PathwayV1GetEndpointResponse
        {
            PathwayId = input.PathwayId,
            Name = input.Name,
            Description = input.Description,
            MinimumYearsWorkExperience = input.MinimumYearsWorkExperience,
            DocumentTypeIds = input.DocumentTypeIds,
            AcceptedEducationLevelIds = input.AcceptedEducationLevelIds,
            Links = []
        };
    }
}
