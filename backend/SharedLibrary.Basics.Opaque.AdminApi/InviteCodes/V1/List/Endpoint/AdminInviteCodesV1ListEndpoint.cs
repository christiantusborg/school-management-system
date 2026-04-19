using SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Endpoint;

[Route("/v1/admin/invite-codes")]
[EndpointTag("AdminInviteCodes")]
public sealed class AdminInviteCodesV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<AdminInviteCodesV1ListCommand, AdminInviteCodesV1ListEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<AdminInviteCodesV1ListCommandResultItem>, AdminInviteCodesV1ListEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AdminInviteCodesV1ListCommand();
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
