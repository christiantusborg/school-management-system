using School.PathwayApi.Pathway.V1.Restore.Command;
using School.PathwayApi.Pathway.V1.Restore.Endpoint;

namespace School.PathwayApi.Pathway.V1.Restore.Endpoint.Mappers;

public sealed class PathwayV1RestoreCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PathwayV1RestoreCommandResult, PathwayV1RestoreEndpointResponse>
{
    public PathwayV1RestoreEndpointResponse MapFrom(PathwayV1RestoreCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PathwayV1RestoreEndpointResponse
        {
            PathwayId = input.PathwayId,
            Links = []
        };
    }
}
