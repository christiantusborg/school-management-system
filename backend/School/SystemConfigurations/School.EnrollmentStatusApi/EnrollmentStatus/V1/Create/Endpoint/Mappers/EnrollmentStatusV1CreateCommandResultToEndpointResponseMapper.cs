using School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Endpoint;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Endpoint.Mappers;

public sealed class EnrollmentStatusV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EnrollmentStatusV1CreateCommandResult, EnrollmentStatusV1CreateEndpointResponse>
{
    public EnrollmentStatusV1CreateEndpointResponse MapFrom(EnrollmentStatusV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EnrollmentStatusV1CreateEndpointResponse
        {
            EnrollmentStatusId = input.EnrollmentStatusId,
            Links = []
        };
    }
}
