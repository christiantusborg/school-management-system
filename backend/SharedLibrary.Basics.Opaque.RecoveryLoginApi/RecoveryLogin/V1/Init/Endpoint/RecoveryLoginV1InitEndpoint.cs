using SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Endpoint;

[Route("/v1/login/recovery/init")]
[EndpointTag("RecoveryLogin")]
public sealed class RecoveryLoginV1InitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<RecoveryLoginV1InitCommand, RecoveryLoginV1InitEndpointResponse>(this, EndpointHandlerAsync)
            .AllowAnonymous();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        RecoveryLoginV1InitEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<RecoveryLoginV1InitCommandResult, RecoveryLoginV1InitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new RecoveryLoginV1InitCommand
        {
            Username = request.Username,
            CodeId = request.CodeId,
            BlindedElement = request.BlindedElement,
            DeviceInfo = request.DeviceInfo
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
