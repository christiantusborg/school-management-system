namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Endpoint;

public sealed class EnrollmentStatusV1ListEndpointResponseItem : HateoasBaseResponse
{
    public required int EnrollmentStatusId { get; init; }
    public required string Name { get; init; }
    public required bool AllowSetByPartner { get; init; }
}
