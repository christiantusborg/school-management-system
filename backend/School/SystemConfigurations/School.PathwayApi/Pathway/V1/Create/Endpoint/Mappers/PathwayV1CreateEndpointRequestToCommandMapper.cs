using School.PathwayApi.Pathway.V1.Create.Command;
using School.PathwayApi.Pathway.V1.Create.Endpoint;

namespace School.PathwayApi.Pathway.V1.Create.Endpoint.Mappers;

public sealed class PathwayV1CreateEndpointRequestToCommandMapper
    : IMapper<PathwayV1CreateEndpointRequest, PathwayV1CreateCommand>
{
    public PathwayV1CreateCommand MapFrom(PathwayV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PathwayV1CreateCommand
        {
            Name = input.Name,
            DocumentTypeIds = input.DocumentTypeIds is null
                ? []
                : input.DocumentTypeIds.Where(id => id > 0).Distinct().ToList(),
        };
    }
}
