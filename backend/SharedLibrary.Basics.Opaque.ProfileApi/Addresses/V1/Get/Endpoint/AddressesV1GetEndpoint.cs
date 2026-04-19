using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Get.Endpoint;

[Route("/v1/profile/addresses/{userAddressId:guid}")]
[EndpointTag("Addresses")]
public sealed class AddressesV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<AddressesV1GetCommand, AddressesV1GetEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userAddressId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AddressesV1GetCommandResult, AddressesV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AddressesV1GetCommand
        {
            UserAddressId = userAddressId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
