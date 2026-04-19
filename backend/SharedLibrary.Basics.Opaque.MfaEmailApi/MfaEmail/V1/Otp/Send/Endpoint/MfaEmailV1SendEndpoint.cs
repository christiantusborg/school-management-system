using Microsoft.AspNetCore.Builder;
using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Endpoint;

[Route("/v1/mfa/email/send")]
[EndpointTag("MfaEmail")]
public sealed class MfaEmailV1SendEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaEmailV1SendCommand, MfaEmailV1SendEndpointResponse>(this, EndpointHandlerAsync)
            .AllowAnonymous();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MfaEmailV1SendEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaEmailV1SendEndpointRequest, MfaEmailV1SendCommand> requestMapper,
        [FromServices] IMapper<MfaEmailV1SendCommandResult, MfaEmailV1SendEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
