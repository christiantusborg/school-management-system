using School.ModeOfStudyApi.ModeOfStudy.V1.Update.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.Update.Endpoint;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Update.Endpoint.Mappers;

public sealed class ModeOfStudyV1UpdateEndpointRequestToCommandMapper
    : IMapper<ModeOfStudyV1UpdateEndpointRequest, ModeOfStudyV1UpdateCommand>
{
    public ModeOfStudyV1UpdateCommand MapFrom(ModeOfStudyV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ModeOfStudyV1UpdateCommand
        {
            ModeOfStudyId = 0,
            Name = input.Name,

        };
    }
}
