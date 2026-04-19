using School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Endpoint;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Endpoint.Mappers;

public sealed class EnrollmentStatusV1PermanentDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EnrollmentStatusV1PermanentDeleteCommandResult, EnrollmentStatusV1PermanentDeleteEndpointResponse>
{
    public EnrollmentStatusV1PermanentDeleteEndpointResponse MapFrom(EnrollmentStatusV1PermanentDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EnrollmentStatusV1PermanentDeleteEndpointResponse
        {
            EnrollmentStatusId = input.EnrollmentStatusId,
            Links = []
        };
    }
}
