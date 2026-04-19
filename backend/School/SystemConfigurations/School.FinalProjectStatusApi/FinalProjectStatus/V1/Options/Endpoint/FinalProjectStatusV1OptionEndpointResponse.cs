namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Options.Endpoint;

public sealed class FinalProjectStatusV1OptionEndpointResponse
{
    public required FinalProjectStatusV1OptionInnerEndpointResponse List { get; init; }
    public required FinalProjectStatusV1OptionInnerEndpointResponse Get { get; init; }
    public required FinalProjectStatusV1OptionInnerEndpointResponse Create { get; init; }
    public required FinalProjectStatusV1OptionInnerEndpointResponse Update { get; init; }
    public required FinalProjectStatusV1OptionInnerEndpointResponse SoftDelete { get; init; }
    public required FinalProjectStatusV1OptionInnerEndpointResponse Restore { get; init; }
    public required FinalProjectStatusV1OptionInnerEndpointResponse PermanentDelete { get; init; }
}
