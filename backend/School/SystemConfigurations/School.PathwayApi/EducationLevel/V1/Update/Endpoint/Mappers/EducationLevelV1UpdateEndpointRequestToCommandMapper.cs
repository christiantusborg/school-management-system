using School.PathwayApi.EducationLevel.V1.Update.Command;

namespace School.PathwayApi.EducationLevel.V1.Update.Endpoint.Mappers;

public sealed class EducationLevelV1UpdateEndpointRequestToCommandMapper
    : IMapper<EducationLevelV1UpdateEndpointRequest, EducationLevelV1UpdateCommand>
{
    public EducationLevelV1UpdateCommand MapFrom(EducationLevelV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EducationLevelV1UpdateCommand
        {
            EducationLevelId = Guid.Empty,
            Name = input.Name,
            Rank = input.Rank,
            DisplayOrder = input.DisplayOrder,
        };
    }
}
