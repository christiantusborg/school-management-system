using School.SubjectApi.Subject.V1.Create.Command;
using School.SubjectApi.Subject.V1.Create.Endpoint;

namespace School.SubjectApi.Subject.V1.Create.Endpoint.Mappers;

public sealed class SubjectV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<SubjectV1CreateCommandResult, SubjectV1CreateEndpointResponse>
{
    public SubjectV1CreateEndpointResponse MapFrom(SubjectV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new SubjectV1CreateEndpointResponse
        {
            SubjectId = input.SubjectId,
            Links = []
        };
    }
}
