using School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Endpoint;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Endpoint.Mappers;

public sealed class EnrollmentStatusV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EnrollmentStatusV1UpdateCommandResult, EnrollmentStatusV1UpdateEndpointResponse>
{
    public EnrollmentStatusV1UpdateEndpointResponse MapFrom(EnrollmentStatusV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EnrollmentStatusV1UpdateEndpointResponse
        {
            EnrollmentStatusId = input.EnrollmentStatusId,
            Links = []
        };
    }
}
