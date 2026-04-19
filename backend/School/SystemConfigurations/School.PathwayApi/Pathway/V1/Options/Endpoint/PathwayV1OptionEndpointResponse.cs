namespace School.PathwayApi.Pathway.V1.Options.Endpoint;

public sealed class PathwayV1OptionEndpointResponse
{
    public required PathwayV1OptionInnerEndpointResponse List { get; init; }
    public required PathwayV1OptionInnerEndpointResponse Get { get; init; }
    public required PathwayV1OptionInnerEndpointResponse Create { get; init; }
    public required PathwayV1OptionInnerEndpointResponse Update { get; init; }
    public required PathwayV1OptionInnerEndpointResponse SoftDelete { get; init; }
    public required PathwayV1OptionInnerEndpointResponse Restore { get; init; }
    public required PathwayV1OptionInnerEndpointResponse PermanentDelete { get; init; }
}
