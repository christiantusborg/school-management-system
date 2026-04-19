using School.ModeOfStudyApi.ModeOfStudy.V1.Update.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.Update.Endpoint;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Update.Endpoint.Mappers;

public sealed class ModeOfStudyV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ModeOfStudyV1UpdateCommandResult, ModeOfStudyV1UpdateEndpointResponse>
{
    public ModeOfStudyV1UpdateEndpointResponse MapFrom(ModeOfStudyV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ModeOfStudyV1UpdateEndpointResponse
        {
            ModeOfStudyId = input.ModeOfStudyId,
            Links = []
        };
    }
}
