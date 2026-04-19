using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Endpoint;

[Route("/v1/password-reset/init")]
[EndpointTag("PasswordReset")]
public sealed class PasswordResetV1InitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<PasswordResetV1InitCommand, PasswordResetV1InitEndpointResponse>(this, EndpointHandlerAsync)
            .AllowAnonymous();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        PasswordResetV1InitEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PasswordResetV1InitCommandResult, PasswordResetV1InitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PasswordResetV1InitCommand
        {
            ResetToken = request.ResetToken,
            BlindedElement = request.BlindedElement
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
