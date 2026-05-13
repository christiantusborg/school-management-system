using School.PathwayApi.Pathway.V1.Update.Command;

namespace School.PathwayApi.Pathway.V1.Update.Endpoint.Mappers;

public sealed class PathwayV1UpdateEndpointRequestToCommandMapper
    : IMapper<PathwayV1UpdateEndpointRequest, PathwayV1UpdateCommand>
{
    public PathwayV1UpdateCommand MapFrom(PathwayV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PathwayV1UpdateCommand
        {
            PathwayId = Guid.Empty,
            Name = input.Name,
            Description = input.Description,
            MinimumYearsWorkExperience = Math.Max(0, input.MinimumYearsWorkExperience),
            DocumentTypeIds = input.DocumentTypeIds ?? Array.Empty<Guid>(),
            AcceptedEducationLevelIds = input.AcceptedEducationLevelIds ?? Array.Empty<Guid>(),
        };
    }
}
