using School.DocumentTypeApi.DocumentType.V1.Restore.Command;

namespace School.DocumentTypeApi.DocumentType.V1.Restore.Endpoint;

[Route("/v1/school/system-config/document-types/{id:int}/restore")]
[EndpointTag("School.SystemConfig.DocumentType")]
public sealed class DocumentTypeV1RestoreEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<DocumentTypeV1RestoreCommand, DocumentTypeV1RestoreEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<DocumentTypeV1RestoreCommandResult, DocumentTypeV1RestoreEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new DocumentTypeV1RestoreCommand { DocumentTypeId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
