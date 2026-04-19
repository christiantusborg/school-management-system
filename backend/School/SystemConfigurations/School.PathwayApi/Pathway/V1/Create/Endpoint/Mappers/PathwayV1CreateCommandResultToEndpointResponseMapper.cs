using School.PathwayApi.Pathway.V1.Create.Command;
using School.PathwayApi.Pathway.V1.Create.Endpoint;

namespace School.PathwayApi.Pathway.V1.Create.Endpoint.Mappers;

public sealed class PathwayV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PathwayV1CreateCommandResult, PathwayV1CreateEndpointResponse>
{
    public PathwayV1CreateEndpointResponse MapFrom(PathwayV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PathwayV1CreateEndpointResponse
        {
            PathwayId = input.PathwayId,
            Links = []
        };
    }
}
