using SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Endpoint;

[Route("/v1/register/finalize")]
[EndpointTag("RegisterFinalize")]
public sealed class RegisterFinalizeV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<RegisterFinalizeV1CreateCommand, RegisterFinalizeV1CreateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] RegisterFinalizeV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<RegisterFinalizeV1CreateEndpointRequest, RegisterFinalizeV1CreateCommand> requestMapper,
        [FromServices] IMapper<RegisterFinalizeV1CreateCommandResult, RegisterFinalizeV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
