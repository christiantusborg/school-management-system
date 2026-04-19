using SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Endpoint;

[Route("/v1/account/mfa/status")]
[EndpointTag("MfaStatus")]
public sealed class MfaStatusV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<MfaStatusV1GetCommand, MfaStatusV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaStatusV1GetCommandResult, MfaStatusV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MfaStatusV1GetCommand { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
