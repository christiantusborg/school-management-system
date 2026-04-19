using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get.Endpoint;

[Route("/v1/admin/users/{userId}")]
[EndpointTag("AdminUsers")]
public sealed class AdminUsersV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<AdminUsersV1GetCommand, AdminUsersV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        string userId,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AdminUsersV1GetCommandResult, AdminUsersV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AdminUsersV1GetCommand { UserId = userId };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
