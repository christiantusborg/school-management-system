using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Endpoint;

[Route("/v1/mfa/email/enable/confirm")]
[EndpointTag("MfaEmail")]
public sealed class MfaEmailV1EnableConfirmEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaEmailV1EnableConfirmCommand, MfaEmailV1EnableConfirmEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MfaEmailV1EnableConfirmEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaEmailV1EnableConfirmEndpointRequest, MfaEmailV1EnableConfirmCommand> requestMapper,
        [FromServices] IMapper<MfaEmailV1EnableConfirmCommandResult, MfaEmailV1EnableConfirmEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
