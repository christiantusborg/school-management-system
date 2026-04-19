using School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Command;

namespace School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Endpoint;

[Route("/v1/school/system-config/document-types/{id:int}/permanent")]
[EndpointTag("School.SystemConfig.DocumentType")]
public sealed class DocumentTypeV1PermanentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<DocumentTypeV1PermanentDeleteCommand, DocumentTypeV1PermanentDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<DocumentTypeV1PermanentDeleteCommandResult, DocumentTypeV1PermanentDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new DocumentTypeV1PermanentDeleteCommand { DocumentTypeId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
