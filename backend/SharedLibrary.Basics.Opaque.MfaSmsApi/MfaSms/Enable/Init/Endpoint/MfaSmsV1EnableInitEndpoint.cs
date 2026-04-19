using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Endpoint;

[Route("/v1/mfa/sms/enable/init")]
[EndpointTag("MfaSms")]
public sealed class MfaSmsV1EnableInitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaSmsV1EnableInitCommand, MfaSmsV1EnableInitEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaSmsV1EnableInitCommandResult, MfaSmsV1EnableInitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MfaSmsV1EnableInitCommand { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
