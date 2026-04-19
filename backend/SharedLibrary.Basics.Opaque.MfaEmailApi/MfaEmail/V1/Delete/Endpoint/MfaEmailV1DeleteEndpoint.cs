using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Endpoint;

[Route("/v1/mfa/email")]
[EndpointTag("MfaEmail")]
public sealed class MfaEmailV1DeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<MfaEmailV1DeleteCommand, MfaEmailV1DeleteEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaEmailV1DeleteCommandResult, MfaEmailV1DeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MfaEmailV1DeleteCommand { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
