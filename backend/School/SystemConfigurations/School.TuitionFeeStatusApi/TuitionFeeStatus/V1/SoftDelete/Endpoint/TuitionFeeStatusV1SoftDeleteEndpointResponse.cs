namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Endpoint;

public sealed class TuitionFeeStatusV1SoftDeleteEndpointResponse : HateoasBaseResponse
{
    public required int TuitionFeeStatusId { get; init; }
}
