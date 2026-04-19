using School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Endpoint;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Endpoint.Mappers;

public sealed class EnrollmentStatusV1RestoreCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EnrollmentStatusV1RestoreCommandResult, EnrollmentStatusV1RestoreEndpointResponse>
{
    public EnrollmentStatusV1RestoreEndpointResponse MapFrom(EnrollmentStatusV1RestoreCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EnrollmentStatusV1RestoreEndpointResponse
        {
            EnrollmentStatusId = input.EnrollmentStatusId,
            Links = []
        };
    }
}
