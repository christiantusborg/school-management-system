using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Endpoint;

[Route("/v1/profile/phones")]
[EndpointTag("Phones")]
public sealed class PhonesV1GetAllEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<PhonesV1GetAllCommand, BaseGetAllResponse<PhonesV1GetAllEndpointResponseItem>>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<PhonesV1GetAllCommandResultItem>, BaseGetAllResponse<PhonesV1GetAllEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PhonesV1GetAllCommand
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
