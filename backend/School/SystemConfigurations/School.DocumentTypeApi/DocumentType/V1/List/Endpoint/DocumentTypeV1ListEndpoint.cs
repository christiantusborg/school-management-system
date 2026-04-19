using School.DocumentTypeApi.DocumentType.V1.List.Command;

namespace School.DocumentTypeApi.DocumentType.V1.List.Endpoint;

[Route("/v1/school/system-config/document-types")]
[EndpointTag("School.SystemConfig.DocumentType")]
public sealed class DocumentTypeV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<DocumentTypeV1ListCommand, BaseGetAllResponse<DocumentTypeV1ListEndpointResponseItem>>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<DocumentTypeV1ListCommandResultItem>, BaseGetAllResponse<DocumentTypeV1ListEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new DocumentTypeV1ListCommand();
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
