using School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Endpoint;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Endpoint.Mappers;

public sealed class EnrollmentStatusV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EnrollmentStatusV1GetCommandResult, EnrollmentStatusV1GetEndpointResponse>
{
    public EnrollmentStatusV1GetEndpointResponse MapFrom(EnrollmentStatusV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EnrollmentStatusV1GetEndpointResponse
        {
            EnrollmentStatusId = input.EnrollmentStatusId,
            Name = input.Name,
            AllowSetByPartner = input.AllowSetByPartner,
            DeletedAt = input.DeletedAt,
            Links = []
        };
    }
}
