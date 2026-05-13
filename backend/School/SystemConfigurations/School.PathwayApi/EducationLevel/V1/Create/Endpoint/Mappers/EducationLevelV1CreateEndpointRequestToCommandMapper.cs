using School.PathwayApi.EducationLevel.V1.Create.Command;

namespace School.PathwayApi.EducationLevel.V1.Create.Endpoint.Mappers;

public sealed class EducationLevelV1CreateEndpointRequestToCommandMapper
    : IMapper<EducationLevelV1CreateEndpointRequest, EducationLevelV1CreateCommand>
{
    public EducationLevelV1CreateCommand MapFrom(EducationLevelV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EducationLevelV1CreateCommand
        {
            Name = input.Name,
            Rank = input.Rank,
            DisplayOrder = input.DisplayOrder,
        };
    }
}
