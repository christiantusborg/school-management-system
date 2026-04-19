using School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Endpoint;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Endpoint.Mappers;

public sealed class ModeOfStudyV1PermanentDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ModeOfStudyV1PermanentDeleteCommandResult, ModeOfStudyV1PermanentDeleteEndpointResponse>
{
    public ModeOfStudyV1PermanentDeleteEndpointResponse MapFrom(ModeOfStudyV1PermanentDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ModeOfStudyV1PermanentDeleteEndpointResponse
        {
            ModeOfStudyId = input.ModeOfStudyId,
            Links = []
        };
    }
}
