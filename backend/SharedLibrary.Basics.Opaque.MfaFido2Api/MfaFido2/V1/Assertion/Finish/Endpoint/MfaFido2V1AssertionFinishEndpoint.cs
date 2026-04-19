using Microsoft.AspNetCore.Builder;
using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Endpoint;

[Route("/v1/mfa/fido2/assertion/finish")]
[EndpointTag("MfaFido2")]
public sealed class MfaFido2V1AssertionFinishEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaFido2V1AssertionFinishCommand, MfaFido2V1AssertionFinishEndpointResponse>(this, EndpointHandlerAsync)
            .AllowAnonymous();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MfaFido2V1AssertionFinishEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaFido2V1AssertionFinishEndpointRequest, MfaFido2V1AssertionFinishCommand> requestMapper,
        [FromServices] IMapper<MfaFido2V1AssertionFinishCommandResult, MfaFido2V1AssertionFinishEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
