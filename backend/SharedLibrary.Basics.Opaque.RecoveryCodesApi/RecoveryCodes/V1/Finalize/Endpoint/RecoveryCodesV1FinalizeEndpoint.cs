using QuVian.SharedLibrary.Basics.SuccessOrFailures.Extensions;
using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Endpoint;

[Route("/v1/recovery-codes/finalize")]
[EndpointTag("RecoveryCodes")]
public sealed class RecoveryCodesV1FinalizeEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<RecoveryCodesV1FinalizeCommand, RecoveryCodesV1FinalizeEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] RecoveryCodesV1FinalizeEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<RecoveryCodesV1FinalizeEndpointRequest, RecoveryCodesV1FinalizeCommand> requestMapper,
        [FromServices] IMapper<RecoveryCodesV1FinalizeCommandResult, RecoveryCodesV1FinalizeEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
