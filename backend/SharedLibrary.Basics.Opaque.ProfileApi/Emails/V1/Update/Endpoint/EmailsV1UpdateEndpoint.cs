using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Endpoint;

[Route("/v1/profile/emails/{userContactEmailId:guid}")]
[EndpointTag("Emails")]
public sealed class EmailsV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<EmailsV1UpdateCommand, EmailsV1UpdateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userContactEmailId,
        [FromBody] EmailsV1UpdateEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EmailsV1UpdateEndpointRequest, EmailsV1UpdateCommand> requestMapper,
        [FromServices] IMapper<EmailsV1UpdateCommandResult, EmailsV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with
        {
            UserContactEmailId = userContactEmailId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
