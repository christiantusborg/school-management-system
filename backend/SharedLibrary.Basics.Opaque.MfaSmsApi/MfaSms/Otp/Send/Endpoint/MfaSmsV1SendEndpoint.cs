using Microsoft.AspNetCore.Builder;
using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Endpoint;

[Route("/v1/mfa/sms/send")]
[EndpointTag("MfaSms")]
public sealed class MfaSmsV1SendEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MfaSmsV1SendCommand, MfaSmsV1SendEndpointResponse>(this, EndpointHandlerAsync)
            .AllowAnonymous();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MfaSmsV1SendEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MfaSmsV1SendEndpointRequest, MfaSmsV1SendCommand> requestMapper,
        [FromServices] IMapper<MfaSmsV1SendCommandResult, MfaSmsV1SendEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
