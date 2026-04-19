using School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Endpoint;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Endpoint.Mappers;

public sealed class EnrollmentStatusV1SoftDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EnrollmentStatusV1SoftDeleteCommandResult, EnrollmentStatusV1SoftDeleteEndpointResponse>
{
    public EnrollmentStatusV1SoftDeleteEndpointResponse MapFrom(EnrollmentStatusV1SoftDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EnrollmentStatusV1SoftDeleteEndpointResponse
        {
            EnrollmentStatusId = input.EnrollmentStatusId,
            Links = []
        };
    }
}
