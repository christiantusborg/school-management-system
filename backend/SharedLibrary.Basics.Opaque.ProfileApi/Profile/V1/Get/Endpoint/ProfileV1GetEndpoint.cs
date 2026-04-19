using SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Endpoint;

[Route("/v1/profile")]
[EndpointTag("Profile")]
public sealed class ProfileV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<ProfileV1GetCommand, ProfileV1GetEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ProfileV1GetCommandResult, ProfileV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new ProfileV1GetCommand
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
