using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Endpoint;

[Route("/v1/profile/emails")]
[EndpointTag("Emails")]
public sealed class EmailsV1GetAllEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<EmailsV1GetAllCommand, BaseGetAllResponse<EmailsV1GetAllEndpointResponseItem>>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<EmailsV1GetAllCommandResultItem>, BaseGetAllResponse<EmailsV1GetAllEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EmailsV1GetAllCommand
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
