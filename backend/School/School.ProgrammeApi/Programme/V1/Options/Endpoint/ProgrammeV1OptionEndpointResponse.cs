namespace School.ProgrammeApi.Programme.V1.Options.Endpoint;

public sealed class ProgrammeV1OptionEndpointResponse
{
    public required ProgrammeV1OptionInnerEndpointResponse List { get; init; }
    public required ProgrammeV1OptionInnerEndpointResponse Get { get; init; }
    public required ProgrammeV1OptionInnerEndpointResponse Create { get; init; }
    public required ProgrammeV1OptionInnerEndpointResponse Update { get; init; }
    public required ProgrammeV1OptionInnerEndpointResponse SoftDelete { get; init; }
    public required ProgrammeV1OptionInnerEndpointResponse Restore { get; init; }
    public required ProgrammeV1OptionInnerEndpointResponse PermanentDelete { get; init; }
}
