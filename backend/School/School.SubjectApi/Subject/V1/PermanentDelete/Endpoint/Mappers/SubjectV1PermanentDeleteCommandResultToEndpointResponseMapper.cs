using School.SubjectApi.Subject.V1.PermanentDelete.Command;
using School.SubjectApi.Subject.V1.PermanentDelete.Endpoint;

namespace School.SubjectApi.Subject.V1.PermanentDelete.Endpoint.Mappers;

public sealed class SubjectV1PermanentDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<SubjectV1PermanentDeleteCommandResult, SubjectV1PermanentDeleteEndpointResponse>
{
    public SubjectV1PermanentDeleteEndpointResponse MapFrom(SubjectV1PermanentDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new SubjectV1PermanentDeleteEndpointResponse
        {
            SubjectId = input.SubjectId,
            Links = []
        };
    }
}
