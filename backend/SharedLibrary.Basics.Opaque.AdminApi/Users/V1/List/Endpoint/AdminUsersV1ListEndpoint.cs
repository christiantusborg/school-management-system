using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Endpoint;

[Route("/v1/admin/users")]
[EndpointTag("AdminUsers")]
public sealed class AdminUsersV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<AdminUsersV1ListCommand, AdminUsersV1ListEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int page,
        int pageSize,
        string? search,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<AdminUsersV1ListCommandResultItem>, AdminUsersV1ListEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AdminUsersV1ListCommand { Page = page, PageSize = pageSize, Search = search };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
