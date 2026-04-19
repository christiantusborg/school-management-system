using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Endpoint;

[Route("/v1/change-password/finalize")]
[EndpointTag("ChangePassword")]
public sealed class ChangePasswordV1FinalizeEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<ChangePasswordV1FinalizeCommand, ChangePasswordV1FinalizeEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] ChangePasswordV1FinalizeEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ChangePasswordV1FinalizeEndpointRequest, ChangePasswordV1FinalizeCommand> requestMapper,
        [FromServices] IMapper<ChangePasswordV1FinalizeCommandResult, ChangePasswordV1FinalizeEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
