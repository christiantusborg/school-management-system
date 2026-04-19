using School.MajorApi.Major.V1.Get.Command;
using School.MajorApi.Major.V1.Get.Endpoint;

namespace School.MajorApi.Major.V1.Get.Endpoint.Mappers;

public sealed class MajorV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MajorV1GetCommandResult, MajorV1GetEndpointResponse>
{
    public MajorV1GetEndpointResponse MapFrom(MajorV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MajorV1GetEndpointResponse
        {
            MajorId = input.MajorId,
            ProgrammeId = input.ProgrammeId,
            Name = input.Name,
            DeletedAt = input.DeletedAt,
            Links = []
        };
    }
}
