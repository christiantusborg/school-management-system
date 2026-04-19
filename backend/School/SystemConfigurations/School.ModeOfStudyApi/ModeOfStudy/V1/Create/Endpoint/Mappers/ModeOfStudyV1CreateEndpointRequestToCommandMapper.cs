using School.ModeOfStudyApi.ModeOfStudy.V1.Create.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.Create.Endpoint;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Create.Endpoint.Mappers;

public sealed class ModeOfStudyV1CreateEndpointRequestToCommandMapper
    : IMapper<ModeOfStudyV1CreateEndpointRequest, ModeOfStudyV1CreateCommand>
{
    public ModeOfStudyV1CreateCommand MapFrom(ModeOfStudyV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ModeOfStudyV1CreateCommand
        {
            Name = input.Name,

        };
    }
}
