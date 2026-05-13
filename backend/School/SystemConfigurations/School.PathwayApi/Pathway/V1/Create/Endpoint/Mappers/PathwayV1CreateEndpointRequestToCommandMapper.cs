using School.PathwayApi.Pathway.V1.Create.Command;

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
            Description = input.Description,
            MinimumYearsWorkExperience = Math.Max(0, input.MinimumYearsWorkExperience),
            DocumentTypeIds = input.DocumentTypeIds ?? Array.Empty<Guid>(),
            AcceptedEducationLevelIds = input.AcceptedEducationLevelIds ?? Array.Empty<Guid>(),
        };
    }
}
