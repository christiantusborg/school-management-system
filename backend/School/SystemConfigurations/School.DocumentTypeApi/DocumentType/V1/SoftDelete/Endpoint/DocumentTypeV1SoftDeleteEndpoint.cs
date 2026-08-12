using Odin.Api.Base.Authorization;
using School.DocumentTypeApi.DocumentType.V1.SoftDelete.Command;

namespace School.DocumentTypeApi.DocumentType.V1.SoftDelete.Endpoint;

[Route("/v1/school/system-config/document-types/{id:guid}")]
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
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<DocumentTypeV1SoftDeleteCommandResult, DocumentTypeV1SoftDeleteEndpointResponse> responseMapper,
        [FromServices] IPermissionService perms,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (await perms.AccessAsync(httpContext.User, "config.lists", cancellationToken) != AccessLevel.Edit) return Results.Forbid();
        var command = new DocumentTypeV1SoftDeleteCommand { DocumentTypeId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
