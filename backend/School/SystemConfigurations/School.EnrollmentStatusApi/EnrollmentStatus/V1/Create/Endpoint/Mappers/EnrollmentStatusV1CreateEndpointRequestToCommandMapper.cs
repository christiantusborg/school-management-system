using School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Endpoint;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Endpoint.Mappers;

public sealed class EnrollmentStatusV1CreateEndpointRequestToCommandMapper
    : IMapper<EnrollmentStatusV1CreateEndpointRequest, EnrollmentStatusV1CreateCommand>
{
    public EnrollmentStatusV1CreateCommand MapFrom(EnrollmentStatusV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EnrollmentStatusV1CreateCommand
        {
            Name = input.Name,
            AllowSetByPartner = input.AllowSetByPartner,
        };
    }
}
