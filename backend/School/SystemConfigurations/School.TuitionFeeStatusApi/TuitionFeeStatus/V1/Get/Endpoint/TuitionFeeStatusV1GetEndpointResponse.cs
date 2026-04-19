namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Endpoint;

public sealed class TuitionFeeStatusV1GetEndpointResponse : HateoasBaseResponse
{
    public required int TuitionFeeStatusId { get; init; }
    public required string Name { get; init; }

    public DateTime? DeletedAt { get; init; }
}
