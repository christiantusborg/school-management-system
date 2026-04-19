using School.PathwayApi.Pathway.V1.PermanentDelete.Command;
using School.PathwayApi.Pathway.V1.PermanentDelete.Endpoint;

namespace School.PathwayApi.Pathway.V1.PermanentDelete.Endpoint.Mappers;

public sealed class PathwayV1PermanentDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PathwayV1PermanentDeleteCommandResult, PathwayV1PermanentDeleteEndpointResponse>
{
    public PathwayV1PermanentDeleteEndpointResponse MapFrom(PathwayV1PermanentDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PathwayV1PermanentDeleteEndpointResponse
        {
            PathwayId = input.PathwayId,
            Links = []
        };
    }
}
