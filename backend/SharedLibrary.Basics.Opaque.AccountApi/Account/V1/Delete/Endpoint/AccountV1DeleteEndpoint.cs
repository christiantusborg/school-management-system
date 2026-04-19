using SharedLibrary.Basics.Opaque.AccountApi.Account.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.AccountApi.Account.V1.Delete.Endpoint;

[Route("/v1/account")]
[EndpointTag("Account")]
public sealed class AccountV1DeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<AccountV1DeleteCommand, AccountV1DeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AccountV1DeleteCommandResult, AccountV1DeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AccountV1DeleteCommand { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
