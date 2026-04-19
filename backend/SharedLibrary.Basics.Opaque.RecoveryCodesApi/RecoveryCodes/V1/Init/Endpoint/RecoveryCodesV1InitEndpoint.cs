using System.Security.Claims;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;
using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Endpoint;

[Route("/v1/recovery-codes/init")]
[EndpointTag("RecoveryCodes")]
public sealed class RecoveryCodesV1InitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<RecoveryCodesV1InitCommand, RecoveryCodesV1InitEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] RecoveryCodesV1InitEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<RecoveryCodesV1InitEndpointRequest, RecoveryCodesV1InitCommand> requestMapper,
        [FromServices] IMapper<RecoveryCodesV1InitCommandResult, RecoveryCodesV1InitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
