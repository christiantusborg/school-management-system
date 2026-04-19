using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Endpoint;

[Route("/v1/profile/emails/{userContactEmailId:guid}/set-primary")]
[EndpointTag("Emails")]
public sealed class EmailsV1SetPrimaryEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<EmailsV1SetPrimaryCommand, EmailsV1SetPrimaryEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userContactEmailId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EmailsV1SetPrimaryCommandResult, EmailsV1SetPrimaryEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EmailsV1SetPrimaryCommand
        {
            UserContactEmailId = userContactEmailId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
