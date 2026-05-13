using School.DocumentTypeApi.DocumentType.V1.Update.Command;

namespace School.DocumentTypeApi.DocumentType.V1.Update.Endpoint;

[Route("/v1/school/system-config/document-types/{id:guid}")]
[EndpointTag("School.SystemConfig.DocumentType")]
public sealed class DocumentTypeV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<DocumentTypeV1UpdateCommand, DocumentTypeV1UpdateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromBody] DocumentTypeV1UpdateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<DocumentTypeV1UpdateEndpointRequest, DocumentTypeV1UpdateCommand> requestMapper,
        [FromServices] IMapper<DocumentTypeV1UpdateCommandResult, DocumentTypeV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { DocumentTypeId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
