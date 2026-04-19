using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole.Endpoint;

[Route("/v1/admin/users/{userId}/change-role")]
[EndpointTag("AdminUsers")]
public sealed class AdminUsersV1ChangeRoleEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<AdminUsersV1ChangeRoleCommand, AdminUsersV1ChangeRoleEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        string userId,
        AdminUsersV1ChangeRoleEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AdminUsersV1ChangeRoleCommandResult, AdminUsersV1ChangeRoleEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AdminUsersV1ChangeRoleCommand { UserId = userId, NewRole = request.NewRole };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
