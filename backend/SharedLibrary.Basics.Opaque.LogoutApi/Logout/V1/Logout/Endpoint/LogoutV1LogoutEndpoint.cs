using SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.Logout.Command;

namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.Logout.Endpoint;

[Route("/v1/logout")]
[EndpointTag("Logout")]
public sealed class LogoutV1LogoutEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<LogoutV1LogoutCommand, LogoutV1LogoutEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<LogoutV1LogoutCommandResult, LogoutV1LogoutEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new LogoutV1LogoutCommand { RawToken = user.FindFirstValue("token")! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
