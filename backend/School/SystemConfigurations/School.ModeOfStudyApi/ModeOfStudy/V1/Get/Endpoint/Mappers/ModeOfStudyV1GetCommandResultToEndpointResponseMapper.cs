using School.ModeOfStudyApi.ModeOfStudy.V1.Get.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.Get.Endpoint;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Get.Endpoint.Mappers;

public sealed class ModeOfStudyV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ModeOfStudyV1GetCommandResult, ModeOfStudyV1GetEndpointResponse>
{
    public ModeOfStudyV1GetEndpointResponse MapFrom(ModeOfStudyV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ModeOfStudyV1GetEndpointResponse
        {
            ModeOfStudyId = input.ModeOfStudyId,
            Name = input.Name,

            DeletedAt = input.DeletedAt,
            Links = []
        };
    }
}
