using School.SubjectApi.Subject.V1.Update.Command;
using School.SubjectApi.Subject.V1.Update.Endpoint;

namespace School.SubjectApi.Subject.V1.Update.Endpoint.Mappers;

public sealed class SubjectV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<SubjectV1UpdateCommandResult, SubjectV1UpdateEndpointResponse>
{
    public SubjectV1UpdateEndpointResponse MapFrom(SubjectV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new SubjectV1UpdateEndpointResponse
        {
            SubjectId = input.SubjectId,
            Links = []
        };
    }
}
