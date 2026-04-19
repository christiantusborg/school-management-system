namespace School.ModeOfStudyApi.ModeOfStudy.V1.Options.Endpoint;

public sealed class ModeOfStudyV1OptionEndpointResponse
{
    public required ModeOfStudyV1OptionInnerEndpointResponse List { get; init; }
    public required ModeOfStudyV1OptionInnerEndpointResponse Get { get; init; }
    public required ModeOfStudyV1OptionInnerEndpointResponse Create { get; init; }
    public required ModeOfStudyV1OptionInnerEndpointResponse Update { get; init; }
    public required ModeOfStudyV1OptionInnerEndpointResponse SoftDelete { get; init; }
    public required ModeOfStudyV1OptionInnerEndpointResponse Restore { get; init; }
    public required ModeOfStudyV1OptionInnerEndpointResponse PermanentDelete { get; init; }
}
