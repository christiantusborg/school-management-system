namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Endpoint;

public sealed class EnrollmentStatusV1CreateEndpointResponse : HateoasBaseResponse
{
    public required int EnrollmentStatusId { get; init; }
}
