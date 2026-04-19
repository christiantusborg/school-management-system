using SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.LogoutEverywhere.Command;

namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.LogoutEverywhere.Endpoint;

[Route("/v1/logout-everywhere")]
[EndpointTag("Logout")]
public sealed class LogoutV1LogoutEverywhereEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<LogoutV1LogoutEverywhereCommand, LogoutV1LogoutEverywhereEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<LogoutV1LogoutEverywhereCommandResult, LogoutV1LogoutEverywhereEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new LogoutV1LogoutEverywhereCommand { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
