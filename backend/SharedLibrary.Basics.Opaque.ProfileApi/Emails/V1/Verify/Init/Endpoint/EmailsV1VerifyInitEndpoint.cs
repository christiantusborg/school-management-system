using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Endpoint;

[Route("/v1/profile/emails/{userContactEmailId:guid}/verify/init")]
[EndpointTag("Emails")]
public sealed class EmailsV1VerifyInitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<EmailsV1VerifyInitCommand, EmailsV1VerifyInitEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromRoute] Guid userContactEmailId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EmailsV1VerifyInitCommandResult, EmailsV1VerifyInitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EmailsV1VerifyInitCommand
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            UserContactEmailId = userContactEmailId
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
