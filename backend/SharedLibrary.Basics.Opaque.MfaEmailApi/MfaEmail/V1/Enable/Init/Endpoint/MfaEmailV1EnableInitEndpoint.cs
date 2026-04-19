using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Endpoint;

[Route("/v1/mfa/email/enable/init")]
[EndpointTag("MfaEmail")]
public sealed class MfaEmailV1EnableInitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaEmailV1EnableInitCommand, MfaEmailV1EnableInitEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaEmailV1EnableInitCommandResult, MfaEmailV1EnableInitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MfaEmailV1EnableInitCommand { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
