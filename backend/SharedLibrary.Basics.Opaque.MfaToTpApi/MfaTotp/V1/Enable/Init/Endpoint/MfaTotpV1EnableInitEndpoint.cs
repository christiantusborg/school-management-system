using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Command;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Endpoint;

[Route("/v1/mfa/totp/enable/init")]
[EndpointTag("MfaTotp")]
public sealed class MfaTotpV1EnableInitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaTotpV1EnableInitCommand, MfaTotpV1EnableInitEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaTotpV1EnableInitCommandResult, MfaTotpV1EnableInitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MfaTotpV1EnableInitCommand
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            Username = user.FindFirstValue(ClaimTypes.Name)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
