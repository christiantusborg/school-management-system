using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Endpoint;

[Route("/v1/profile/phones/{userPhoneId:guid}")]
[EndpointTag("Phones")]
public sealed class PhonesV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<PhonesV1GetCommand, PhonesV1GetEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userPhoneId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PhonesV1GetCommandResult, PhonesV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PhonesV1GetCommand
        {
            UserPhoneId = userPhoneId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
