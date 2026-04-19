using School.SubjectApi.Subject.V1.Restore.Command;
using School.SubjectApi.Subject.V1.Restore.Endpoint;

namespace School.SubjectApi.Subject.V1.Restore.Endpoint.Mappers;

public sealed class SubjectV1RestoreCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<SubjectV1RestoreCommandResult, SubjectV1RestoreEndpointResponse>
{
    public SubjectV1RestoreEndpointResponse MapFrom(SubjectV1RestoreCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new SubjectV1RestoreEndpointResponse
        {
            SubjectId = input.SubjectId,
            Links = []
        };
    }
}
