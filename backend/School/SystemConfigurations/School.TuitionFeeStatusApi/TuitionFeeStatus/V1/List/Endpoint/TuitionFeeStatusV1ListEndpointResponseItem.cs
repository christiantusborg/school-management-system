namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Endpoint;

public sealed class TuitionFeeStatusV1ListEndpointResponseItem : HateoasBaseResponse
{
    public required int TuitionFeeStatusId { get; init; }
    public required string Name { get; init; }

}
