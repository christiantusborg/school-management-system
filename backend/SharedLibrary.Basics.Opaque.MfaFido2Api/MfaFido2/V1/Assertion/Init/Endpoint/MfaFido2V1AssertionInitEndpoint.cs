using Microsoft.AspNetCore.Builder;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Endpoint;

[Route("/v1/mfa/fido2/assertion/init")]
[EndpointTag("MfaFido2")]
public sealed class MfaFido2V1AssertionInitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaFido2V1AssertionInitCommand, MfaFido2V1AssertionInitEndpointResponse>(this, EndpointHandlerAsync)
            .AllowAnonymous();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MfaFido2V1AssertionInitEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaFido2V1AssertionInitEndpointRequest, MfaFido2V1AssertionInitCommand> requestMapper,
        [FromServices] IMapper<MfaFido2V1AssertionInitCommandResult, MfaFido2V1AssertionInitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
