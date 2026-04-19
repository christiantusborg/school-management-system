using School.MajorApi.Major.V1.Restore.Command;
using School.MajorApi.Major.V1.Restore.Endpoint;

namespace School.MajorApi.Major.V1.Restore.Endpoint.Mappers;

public sealed class MajorV1RestoreCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MajorV1RestoreCommandResult, MajorV1RestoreEndpointResponse>
{
    public MajorV1RestoreEndpointResponse MapFrom(MajorV1RestoreCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MajorV1RestoreEndpointResponse
        {
            MajorId = input.MajorId,
            Links = []
        };
    }
}
