using School.SubjectApi.Subject.V1.Get.Command;
using School.SubjectApi.Subject.V1.Get.Endpoint;

namespace School.SubjectApi.Subject.V1.Get.Endpoint.Mappers;

public sealed class SubjectV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<SubjectV1GetCommandResult, SubjectV1GetEndpointResponse>
{
    public SubjectV1GetEndpointResponse MapFrom(SubjectV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new SubjectV1GetEndpointResponse
        {
            SubjectId = input.SubjectId,
            MajorId = input.MajorId,
            Code = input.Code,
            Name = input.Name,
            Ects = input.Ects,
            DeletedAt = input.DeletedAt,
            Links = []
        };
    }
}
