using School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Endpoint;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Endpoint.Mappers;

public sealed class EnrollmentStatusV1UpdateEndpointRequestToCommandMapper
    : IMapper<EnrollmentStatusV1UpdateEndpointRequest, EnrollmentStatusV1UpdateCommand>
{
    public EnrollmentStatusV1UpdateCommand MapFrom(EnrollmentStatusV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EnrollmentStatusV1UpdateCommand
        {
            EnrollmentStatusId = 0,
            Name = input.Name,
            AllowSetByPartner = input.AllowSetByPartner,
        };
    }
}
