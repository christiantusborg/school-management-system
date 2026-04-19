namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Endpoint;

public sealed class EnrollmentStatusV1SoftDeleteEndpointResponse : HateoasBaseResponse
{
    public required int EnrollmentStatusId { get; init; }
}
