using Microsoft.AspNetCore.Builder;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Endpoint;

[Route("/v1/mfa/sms/verify")]
[EndpointTag("MfaSms")]
public sealed class MfaSmsV1VerifyEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaSmsV1VerifyCommand, MfaSmsV1VerifyEndpointResponse>(this, EndpointHandlerAsync)
            .AllowAnonymous();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MfaSmsV1VerifyEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaSmsV1VerifyEndpointRequest, MfaSmsV1VerifyCommand> requestMapper,
        [FromServices] IMapper<MfaSmsV1VerifyCommandResult, MfaSmsV1VerifyEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
