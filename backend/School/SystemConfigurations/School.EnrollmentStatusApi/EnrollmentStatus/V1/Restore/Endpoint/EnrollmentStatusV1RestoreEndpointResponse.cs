namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Endpoint;

public sealed class EnrollmentStatusV1RestoreEndpointResponse : HateoasBaseResponse
{
    public required int EnrollmentStatusId { get; init; }
}
