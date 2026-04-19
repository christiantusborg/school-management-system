using SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Endpoint;

[Route("/v1/admin/invite-codes")]
[EndpointTag("AdminInviteCodes")]
public sealed class AdminInviteCodesV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<AdminInviteCodesV1CreateCommand, AdminInviteCodesV1CreateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        AdminInviteCodesV1CreateEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AdminInviteCodesV1CreateCommandResult, AdminInviteCodesV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AdminInviteCodesV1CreateCommand
        {
            CreatedByUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            AssignedRole = request.AssignedRole,
            ExpirationDays = request.ExpirationDays
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
