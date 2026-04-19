using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Endpoint;

[Route("/v1/mfa/fido2/register/finish")]
[EndpointTag("MfaFido2")]
public sealed class MfaFido2V1RegisterFinishEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaFido2V1RegisterFinishCommand, MfaFido2V1RegisterFinishEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MfaFido2V1RegisterFinishEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaFido2V1RegisterFinishEndpointRequest, MfaFido2V1RegisterFinishCommand> requestMapper,
        [FromServices] IMapper<MfaFido2V1RegisterFinishCommandResult, MfaFido2V1RegisterFinishEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
