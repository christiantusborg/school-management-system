using School.MajorApi.Major.V1.PermanentDelete.Command;
using School.MajorApi.Major.V1.PermanentDelete.Endpoint;

namespace School.MajorApi.Major.V1.PermanentDelete.Endpoint.Mappers;

public sealed class MajorV1PermanentDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MajorV1PermanentDeleteCommandResult, MajorV1PermanentDeleteEndpointResponse>
{
    public MajorV1PermanentDeleteEndpointResponse MapFrom(MajorV1PermanentDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MajorV1PermanentDeleteEndpointResponse
        {
            MajorId = input.MajorId,
            Links = []
        };
    }
}
