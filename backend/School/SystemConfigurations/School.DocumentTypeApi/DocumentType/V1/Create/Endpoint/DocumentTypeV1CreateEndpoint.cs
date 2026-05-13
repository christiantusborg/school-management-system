using School.DocumentTypeApi.DocumentType.V1.Create.Command;

namespace School.DocumentTypeApi.DocumentType.V1.Create.Endpoint;

[Route("/v1/school/system-config/document-types")]
[EndpointTag("School.SystemConfig.DocumentType")]
public sealed class DocumentTypeV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<DocumentTypeV1CreateCommand, DocumentTypeV1CreateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] DocumentTypeV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<DocumentTypeV1CreateEndpointRequest, DocumentTypeV1CreateCommand> requestMapper,
        [FromServices] IMapper<DocumentTypeV1CreateCommandResult, DocumentTypeV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
