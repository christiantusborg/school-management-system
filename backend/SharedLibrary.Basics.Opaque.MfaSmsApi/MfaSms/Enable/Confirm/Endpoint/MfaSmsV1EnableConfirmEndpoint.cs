using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Endpoint;

[Route("/v1/mfa/sms/enable/confirm")]
[EndpointTag("MfaSms")]
public sealed class MfaSmsV1EnableConfirmEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaSmsV1EnableConfirmCommand, MfaSmsV1EnableConfirmEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MfaSmsV1EnableConfirmEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaSmsV1EnableConfirmEndpointRequest, MfaSmsV1EnableConfirmCommand> requestMapper,
        [FromServices] IMapper<MfaSmsV1EnableConfirmCommandResult, MfaSmsV1EnableConfirmEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
