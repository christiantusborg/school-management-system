namespace School.PathwayApi.EducationLevel.V1.Options.Endpoint;

public sealed class EducationLevelV1OptionEndpointResponse
{
    public required EducationLevelV1OptionInnerEndpointResponse List { get; init; }
    public required EducationLevelV1OptionInnerEndpointResponse Create { get; init; }
    public required EducationLevelV1OptionInnerEndpointResponse Update { get; init; }
    public required EducationLevelV1OptionInnerEndpointResponse SoftDelete { get; init; }
}
