namespace School.DocumentTypeApi.DocumentType.V1.Options.Endpoint;

public sealed class DocumentTypeV1OptionEndpointResponse
{
    public required DocumentTypeV1OptionInnerEndpointResponse List { get; init; }
    public required DocumentTypeV1OptionInnerEndpointResponse Create { get; init; }
    public required DocumentTypeV1OptionInnerEndpointResponse Update { get; init; }
    public required DocumentTypeV1OptionInnerEndpointResponse SoftDelete { get; init; }
}
