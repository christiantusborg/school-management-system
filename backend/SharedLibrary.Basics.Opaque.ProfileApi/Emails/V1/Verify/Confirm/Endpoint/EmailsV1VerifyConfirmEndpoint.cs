using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Endpoint;

[Route("/v1/profile/emails/verify/confirm")]
[EndpointTag("Emails")]
public sealed class EmailsV1VerifyConfirmEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<EmailsV1VerifyConfirmCommand, EmailsV1VerifyConfirmEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] EmailsV1VerifyConfirmEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EmailsV1VerifyConfirmEndpointRequest, EmailsV1VerifyConfirmCommand> requestMapper,
        [FromServices] IMapper<EmailsV1VerifyConfirmCommandResult, EmailsV1VerifyConfirmEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
