using School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Endpoint;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Endpoint.Mappers;

public sealed class ModeOfStudyV1RestoreCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ModeOfStudyV1RestoreCommandResult, ModeOfStudyV1RestoreEndpointResponse>
{
    public ModeOfStudyV1RestoreEndpointResponse MapFrom(ModeOfStudyV1RestoreCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ModeOfStudyV1RestoreEndpointResponse
        {
            ModeOfStudyId = input.ModeOfStudyId,
            Links = []
        };
    }
}
