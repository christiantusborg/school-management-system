using School.ModeOfStudyApi.ModeOfStudy.V1.Create.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.Create.Endpoint;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Create.Endpoint.Mappers;

public sealed class ModeOfStudyV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ModeOfStudyV1CreateCommandResult, ModeOfStudyV1CreateEndpointResponse>
{
    public ModeOfStudyV1CreateEndpointResponse MapFrom(ModeOfStudyV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ModeOfStudyV1CreateEndpointResponse
        {
            ModeOfStudyId = input.ModeOfStudyId,
            Links = []
        };
    }
}
