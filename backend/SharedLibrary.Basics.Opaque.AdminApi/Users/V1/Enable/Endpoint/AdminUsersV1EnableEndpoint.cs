using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Enable.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Enable.Endpoint;

[Route("/v1/admin/users/{userId}/enable")]
[EndpointTag("AdminUsers")]
public sealed class AdminUsersV1EnableEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<AdminUsersV1EnableCommand, AdminUsersV1EnableEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        string userId,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AdminUsersV1EnableCommandResult, AdminUsersV1EnableEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AdminUsersV1EnableCommand { UserId = userId };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
