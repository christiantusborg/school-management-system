namespace School.DocumentTypeApi.DocumentType.V1.Options.Endpoint;

public sealed class DocumentTypeV1OptionEndpointResponse
{
    public required DocumentTypeV1OptionInnerEndpointResponse List { get; init; }
    public required DocumentTypeV1OptionInnerEndpointResponse Get { get; init; }
    public required DocumentTypeV1OptionInnerEndpointResponse Create { get; init; }
    public required DocumentTypeV1OptionInnerEndpointResponse Update { get; init; }
    public required DocumentTypeV1OptionInnerEndpointResponse SoftDelete { get; init; }
    public required DocumentTypeV1OptionInnerEndpointResponse Restore { get; init; }
    public required DocumentTypeV1OptionInnerEndpointResponse PermanentDelete { get; init; }
}