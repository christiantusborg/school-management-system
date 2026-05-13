using School.PathwayApi.Pathway.V1.SoftDelete.Command;

namespace School.PathwayApi.Pathway.V1.SoftDelete.Endpoint.Mappers;

public sealed class PathwayV1SoftDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PathwayV1SoftDeleteCommandResult, PathwayV1SoftDeleteEndpointResponse>
{
    public PathwayV1SoftDeleteEndpointResponse MapFrom(PathwayV1SoftDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PathwayV1SoftDeleteEndpointResponse
        {
            PathwayId = input.PathwayId,
            Links = []
        };
    }
}
