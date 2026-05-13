using School.PathwayApi.Pathway.V1.Update.Command;

namespace School.PathwayApi.Pathway.V1.Update.Endpoint.Mappers;

public sealed class PathwayV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PathwayV1UpdateCommandResult, PathwayV1UpdateEndpointResponse>
{
    public PathwayV1UpdateEndpointResponse MapFrom(PathwayV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PathwayV1UpdateEndpointResponse
        {
            PathwayId = input.PathwayId,
            Links = []
        };
    }
}
