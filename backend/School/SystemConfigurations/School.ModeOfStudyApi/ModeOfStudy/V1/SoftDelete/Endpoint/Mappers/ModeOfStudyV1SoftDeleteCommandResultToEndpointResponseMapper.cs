using School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Endpoint;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Endpoint.Mappers;

public sealed class ModeOfStudyV1SoftDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ModeOfStudyV1SoftDeleteCommandResult, ModeOfStudyV1SoftDeleteEndpointResponse>
{
    public ModeOfStudyV1SoftDeleteEndpointResponse MapFrom(ModeOfStudyV1SoftDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ModeOfStudyV1SoftDeleteEndpointResponse
        {
            ModeOfStudyId = input.ModeOfStudyId,
            Links = []
        };
    }
}
