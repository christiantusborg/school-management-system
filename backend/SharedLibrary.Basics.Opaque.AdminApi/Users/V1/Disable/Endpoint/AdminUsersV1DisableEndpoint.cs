using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Disable.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Disable.Endpoint;

[Route("/v1/admin/users/{userId}/disable")]
[EndpointTag("AdminUsers")]
public sealed class AdminUsersV1DisableEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<AdminUsersV1DisableCommand, AdminUsersV1DisableEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        string userId,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AdminUsersV1DisableCommandResult, AdminUsersV1DisableEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AdminUsersV1DisableCommand { UserId = userId };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
