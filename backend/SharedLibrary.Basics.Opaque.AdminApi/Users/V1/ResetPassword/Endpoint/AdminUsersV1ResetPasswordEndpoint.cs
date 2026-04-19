using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Endpoint;

[Route("/v1/admin/users/{userId}/reset-password")]
[EndpointTag("AdminUsers")]
public sealed class AdminUsersV1ResetPasswordEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<AdminUsersV1ResetPasswordCommand, AdminUsersV1ResetPasswordEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        string userId,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AdminUsersV1ResetPasswordCommandResult, AdminUsersV1ResetPasswordEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AdminUsersV1ResetPasswordCommand { UserId = userId };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
