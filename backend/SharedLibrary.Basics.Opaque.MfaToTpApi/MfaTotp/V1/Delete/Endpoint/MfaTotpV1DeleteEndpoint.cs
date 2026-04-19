using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Endpoint;

[Route("/v1/mfa/totp")]
[EndpointTag("MfaTotp")]
public sealed class MfaTotpV1DeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<MfaTotpV1DeleteCommand, MfaTotpV1DeleteEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaTotpV1DeleteCommandResult, MfaTotpV1DeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MfaTotpV1DeleteCommand { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
