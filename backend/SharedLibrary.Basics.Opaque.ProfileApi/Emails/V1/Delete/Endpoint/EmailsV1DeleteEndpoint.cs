using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Endpoint;

[Route("/v1/profile/emails/{userContactEmailId:guid}")]
[EndpointTag("Emails")]
public sealed class EmailsV1DeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<EmailsV1DeleteCommand, EmailsV1DeleteEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userContactEmailId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EmailsV1DeleteCommandResult, EmailsV1DeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EmailsV1DeleteCommand
        {
            UserContactEmailId = userContactEmailId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
