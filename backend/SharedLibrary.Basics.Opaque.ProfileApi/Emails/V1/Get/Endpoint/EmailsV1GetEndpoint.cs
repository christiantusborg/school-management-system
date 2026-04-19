using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get.Endpoint;

[Route("/v1/profile/emails/{userContactEmailId:guid}")]
[EndpointTag("Emails")]
public sealed class EmailsV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<EmailsV1GetCommand, EmailsV1GetEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userContactEmailId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EmailsV1GetCommandResult, EmailsV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EmailsV1GetCommand
        {
            UserContactEmailId = userContactEmailId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
