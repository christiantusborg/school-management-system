namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Endpoint;

public sealed class EnrollmentStatusV1PermanentDeleteEndpointResponse : HateoasBaseResponse
{
    public required int EnrollmentStatusId { get; init; }
}
