using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Endpoint;

[Route("/v1/mfa/fido2/{fido2CredentialId:guid}")]
[EndpointTag("MfaFido2")]
public sealed class MfaFido2V1DeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<MfaFido2V1DeleteCommand, MfaFido2V1DeleteEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid fido2CredentialId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaFido2V1DeleteCommandResult, MfaFido2V1DeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MfaFido2V1DeleteCommand
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            Fido2CredentialId = fido2CredentialId
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
