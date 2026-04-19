using SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Endpoint;

[Route("/v1/profile")]
[EndpointTag("Profile")]
public sealed class ProfileV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<ProfileV1UpdateCommand, ProfileV1UpdateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] ProfileV1UpdateEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ProfileV1UpdateEndpointRequest, ProfileV1UpdateCommand> requestMapper,
        [FromServices] IMapper<ProfileV1UpdateCommandResult, ProfileV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
