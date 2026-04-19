using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Endpoint;

[Route("/v1/password-reset/finalize")]
[EndpointTag("PasswordReset")]
public sealed class PasswordResetV1FinalizeEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<PasswordResetV1FinalizeCommand, PasswordResetV1FinalizeEndpointResponse>(this, EndpointHandlerAsync)
            .AllowAnonymous();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        PasswordResetV1FinalizeEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PasswordResetV1FinalizeCommandResult, PasswordResetV1FinalizeEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PasswordResetV1FinalizeCommand
        {
            ResetId = request.ResetId,
            ClientPublicKey = request.ClientPublicKey
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
