using School.SubjectApi.Subject.V1.SoftDelete.Command;
using School.SubjectApi.Subject.V1.SoftDelete.Endpoint;

namespace School.SubjectApi.Subject.V1.SoftDelete.Endpoint.Mappers;

public sealed class SubjectV1SoftDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<SubjectV1SoftDeleteCommandResult, SubjectV1SoftDeleteEndpointResponse>
{
    public SubjectV1SoftDeleteEndpointResponse MapFrom(SubjectV1SoftDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new SubjectV1SoftDeleteEndpointResponse
        {
            SubjectId = input.SubjectId,
            Links = []
        };
    }
}
