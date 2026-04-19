using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Endpoint;

[Route("/v1/profile/addresses")]
[EndpointTag("Addresses")]
public sealed class AddressesV1GetAllEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<AddressesV1GetAllCommand, BaseGetAllResponse<AddressesV1GetAllEndpointResponseItem>>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<AddressesV1GetAllCommandResultItem>, BaseGetAllResponse<AddressesV1GetAllEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AddressesV1GetAllCommand
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
