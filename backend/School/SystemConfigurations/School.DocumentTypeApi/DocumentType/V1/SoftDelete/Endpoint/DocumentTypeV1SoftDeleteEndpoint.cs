using School.DocumentTypeApi.DocumentType.V1.SoftDelete.Command;

namespace School.DocumentTypeApi.DocumentType.V1.SoftDelete.Endpoint;

[Route("/v1/school/system-config/document-types/{id:int}")]
[EndpointTag("School.SystemConfig.DocumentType")]
public sealed class DocumentTypeV1SoftDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<DocumentTypeV1SoftDeleteCommand, DocumentTypeV1SoftDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<DocumentTypeV1SoftDeleteCommandResult, DocumentTypeV1SoftDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new DocumentTypeV1SoftDeleteCommand { DocumentTypeId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
