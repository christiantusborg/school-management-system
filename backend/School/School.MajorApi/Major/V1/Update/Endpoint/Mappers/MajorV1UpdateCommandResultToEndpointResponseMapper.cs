using School.MajorApi.Major.V1.Update.Command;
using School.MajorApi.Major.V1.Update.Endpoint;

namespace School.MajorApi.Major.V1.Update.Endpoint.Mappers;

public sealed class MajorV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MajorV1UpdateCommandResult, MajorV1UpdateEndpointResponse>
{
    public MajorV1UpdateEndpointResponse MapFrom(MajorV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MajorV1UpdateEndpointResponse
        {
            MajorId = input.MajorId,
            Links = []
        };
    }
}
