using SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Command;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Endpoint;

[Route("/v1/users/search")]
[EndpointTag("Users")]
public sealed class UsersV1SearchEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<UsersV1SearchCommand, UsersV1SearchEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromQuery] string q,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<UsersV1SearchCommandResult, UsersV1SearchEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new UsersV1SearchCommand { Query = q ?? string.Empty };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
