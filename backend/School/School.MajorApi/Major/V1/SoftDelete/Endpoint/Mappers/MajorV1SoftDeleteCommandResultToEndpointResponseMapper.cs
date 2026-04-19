using School.MajorApi.Major.V1.SoftDelete.Command;
using School.MajorApi.Major.V1.SoftDelete.Endpoint;

namespace School.MajorApi.Major.V1.SoftDelete.Endpoint.Mappers;

public sealed class MajorV1SoftDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MajorV1SoftDeleteCommandResult, MajorV1SoftDeleteEndpointResponse>
{
    public MajorV1SoftDeleteEndpointResponse MapFrom(MajorV1SoftDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MajorV1SoftDeleteEndpointResponse
        {
            MajorId = input.MajorId,
            Links = []
        };
    }
}
