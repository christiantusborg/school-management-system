using Microsoft.AspNetCore.Builder;
using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Command;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Endpoint;

[Route("/v1/mfa/totp/verify")]
[EndpointTag("MfaTotp")]
public sealed class MfaTotpV1VerifyEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaTotpV1VerifyCommand, MfaTotpV1VerifyEndpointResponse>(this, EndpointHandlerAsync)
            .AllowAnonymous();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MfaTotpV1VerifyEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaTotpV1VerifyEndpointRequest, MfaTotpV1VerifyCommand> requestMapper,
        [FromServices] IMapper<MfaTotpV1VerifyCommandResult, MfaTotpV1VerifyEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
