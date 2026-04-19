using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Endpoint;

[Route("/v1/mfa/sms")]
[EndpointTag("MfaSms")]
public sealed class MfaSmsV1DeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<MfaSmsV1DeleteCommand, MfaSmsV1DeleteEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaSmsV1DeleteCommandResult, MfaSmsV1DeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MfaSmsV1DeleteCommand { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
