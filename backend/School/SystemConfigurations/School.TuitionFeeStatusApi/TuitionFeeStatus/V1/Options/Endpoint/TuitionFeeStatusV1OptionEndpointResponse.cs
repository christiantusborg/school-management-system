namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Options.Endpoint;

public sealed class TuitionFeeStatusV1OptionEndpointResponse
{
    public required TuitionFeeStatusV1OptionInnerEndpointResponse List { get; init; }
    public required TuitionFeeStatusV1OptionInnerEndpointResponse Get { get; init; }
    public required TuitionFeeStatusV1OptionInnerEndpointResponse Create { get; init; }
    public required TuitionFeeStatusV1OptionInnerEndpointResponse Update { get; init; }
    public required TuitionFeeStatusV1OptionInnerEndpointResponse SoftDelete { get; init; }
    public required TuitionFeeStatusV1OptionInnerEndpointResponse Restore { get; init; }
    public required TuitionFeeStatusV1OptionInnerEndpointResponse PermanentDelete { get; init; }
}
