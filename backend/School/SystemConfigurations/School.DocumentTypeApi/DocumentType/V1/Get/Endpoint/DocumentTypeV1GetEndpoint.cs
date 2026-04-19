using School.DocumentTypeApi.DocumentType.V1.Get.Command;

namespace School.DocumentTypeApi.DocumentType.V1.Get.Endpoint;

[Route("/v1/school/system-config/document-types/{id:int}")]
[EndpointTag("School.SystemConfig.DocumentType")]
public sealed class DocumentTypeV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<DocumentTypeV1GetCommand, DocumentTypeV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<DocumentTypeV1GetCommandResult, DocumentTypeV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new DocumentTypeV1GetCommand { DocumentTypeId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
