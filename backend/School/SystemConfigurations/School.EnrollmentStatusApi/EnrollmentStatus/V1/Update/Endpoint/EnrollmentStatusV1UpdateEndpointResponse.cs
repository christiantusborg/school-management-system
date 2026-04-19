namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Endpoint;

public sealed class EnrollmentStatusV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required int EnrollmentStatusId { get; init; }
}
