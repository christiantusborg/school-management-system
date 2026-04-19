using System.Security.Claims;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;
using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Command;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Endpoint;

[Route("/v1/recovery-codes")]
[EndpointTag("RecoveryCodes")]
public sealed class RecoveryCodesV1GetStatusEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<RecoveryCodesV1GetStatusCommand, RecoveryCodesV1GetStatusEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<RecoveryCodesV1GetStatusCommandResult, RecoveryCodesV1GetStatusEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new RecoveryCodesV1GetStatusCommand { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
