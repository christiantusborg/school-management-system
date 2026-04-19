namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Endpoint;

public sealed class EnrollmentStatusV1GetEndpointResponse : HateoasBaseResponse
{
    public required int EnrollmentStatusId { get; init; }
    public required string Name { get; init; }
    public required bool AllowSetByPartner { get; init; }
    public DateTime? DeletedAt { get; init; }
}
