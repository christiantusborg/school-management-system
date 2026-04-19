using School.MajorApi.Major.V1.Create.Command;
using School.MajorApi.Major.V1.Create.Endpoint;

namespace School.MajorApi.Major.V1.Create.Endpoint.Mappers;

public sealed class MajorV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MajorV1CreateCommandResult, MajorV1CreateEndpointResponse>
{
    public MajorV1CreateEndpointResponse MapFrom(MajorV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MajorV1CreateEndpointResponse
        {
            MajorId = input.MajorId,
            Links = []
        };
    }
}
