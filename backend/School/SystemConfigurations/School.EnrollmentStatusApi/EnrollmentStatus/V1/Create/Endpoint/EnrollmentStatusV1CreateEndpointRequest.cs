namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Endpoint;

public sealed class EnrollmentStatusV1CreateEndpointRequest
{
    public required string Name { get; init; }
    public bool AllowSetByPartner { get; init; }
}
