namespace School.MajorApi.Major.V1.Options.Endpoint;

public sealed class MajorV1OptionEndpointResponse
{
    public required MajorV1OptionInnerEndpointResponse List { get; init; }
    public required MajorV1OptionInnerEndpointResponse Get { get; init; }
    public required MajorV1OptionInnerEndpointResponse Create { get; init; }
    public required MajorV1OptionInnerEndpointResponse Update { get; init; }
    public required MajorV1OptionInnerEndpointResponse SoftDelete { get; init; }
    public required MajorV1OptionInnerEndpointResponse Restore { get; init; }
    public required MajorV1OptionInnerEndpointResponse PermanentDelete { get; init; }
}
