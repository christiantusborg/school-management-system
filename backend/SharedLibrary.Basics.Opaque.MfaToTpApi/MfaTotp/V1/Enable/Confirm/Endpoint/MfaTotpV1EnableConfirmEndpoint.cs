using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm.Endpoint;

[Route("/v1/mfa/totp/enable/confirm")]
[EndpointTag("MfaTotp")]
public sealed class MfaTotpV1EnableConfirmEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaTotpV1EnableConfirmCommand, MfaTotpV1EnableConfirmEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MfaTotpV1EnableConfirmEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaTotpV1EnableConfirmCommandResult, MfaTotpV1EnableConfirmEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MfaTotpV1EnableConfirmCommand
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            Code = request.Code
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
