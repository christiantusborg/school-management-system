namespace School.SubjectApi.Subject.V1.Options.Endpoint;

public sealed class SubjectV1OptionEndpointResponse
{
    public required SubjectV1OptionInnerEndpointResponse List { get; init; }
    public required SubjectV1OptionInnerEndpointResponse Get { get; init; }
    public required SubjectV1OptionInnerEndpointResponse Create { get; init; }
    public required SubjectV1OptionInnerEndpointResponse Update { get; init; }
    public required SubjectV1OptionInnerEndpointResponse SoftDelete { get; init; }
    public required SubjectV1OptionInnerEndpointResponse Restore { get; init; }
    public required SubjectV1OptionInnerEndpointResponse PermanentDelete { get; init; }
}
