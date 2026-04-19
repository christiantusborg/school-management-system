using Microsoft.AspNetCore.Builder;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Endpoint;

[Route("/v1/mfa/email/verify")]
[EndpointTag("MfaEmail")]
public sealed class MfaEmailV1VerifyEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaEmailV1VerifyCommand, MfaEmailV1VerifyEndpointResponse>(this, EndpointHandlerAsync)
            .AllowAnonymous();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MfaEmailV1VerifyEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaEmailV1VerifyEndpointRequest, MfaEmailV1VerifyCommand> requestMapper,
        [FromServices] IMapper<MfaEmailV1VerifyCommandResult, MfaEmailV1VerifyEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
