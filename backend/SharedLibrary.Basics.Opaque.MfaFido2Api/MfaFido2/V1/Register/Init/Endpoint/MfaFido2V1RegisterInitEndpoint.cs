using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Endpoint;

[Route("/v1/mfa/fido2/register/init")]
[EndpointTag("MfaFido2")]
public sealed class MfaFido2V1RegisterInitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaFido2V1RegisterInitCommand, MfaFido2V1RegisterInitEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaFido2V1RegisterInitCommandResult, MfaFido2V1RegisterInitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MfaFido2V1RegisterInitCommand
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            Username = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
