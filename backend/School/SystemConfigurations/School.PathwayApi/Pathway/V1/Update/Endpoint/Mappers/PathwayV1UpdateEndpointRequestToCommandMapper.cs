using School.PathwayApi.Pathway.V1.Update.Command;
using School.PathwayApi.Pathway.V1.Update.Endpoint;

namespace School.PathwayApi.Pathway.V1.Update.Endpoint.Mappers;

public sealed class PathwayV1UpdateEndpointRequestToCommandMapper
    : IMapper<PathwayV1UpdateEndpointRequest, PathwayV1UpdateCommand>
{
    public PathwayV1UpdateCommand MapFrom(PathwayV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PathwayV1UpdateCommand
        {
            PathwayId = 0,
            Name = input.Name,
            DocumentTypeIds = input.DocumentTypeIds is null
                ? []
                : input.DocumentTypeIds.Where(id => id > 0).Distinct().ToList(),
        };
    }
}
