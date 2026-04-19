namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Endpoint;

public sealed class EnrollmentStatusV1UpdateEndpointRequest
{
    public required string Name { get; init; }
    public bool AllowSetByPartner { get; init; }
}
