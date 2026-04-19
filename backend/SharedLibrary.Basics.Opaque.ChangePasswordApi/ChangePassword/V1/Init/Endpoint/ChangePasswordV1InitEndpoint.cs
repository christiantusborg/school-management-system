using System.Security.Claims;
using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Endpoint;

[Route("/v1/change-password/init")]
[EndpointTag("ChangePassword")]
public sealed class ChangePasswordV1InitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<ChangePasswordV1InitCommand, ChangePasswordV1InitEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] ChangePasswordV1InitEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ChangePasswordV1InitEndpointRequest, ChangePasswordV1InitCommand> requestMapper,
        [FromServices] IMapper<ChangePasswordV1InitCommandResult, ChangePasswordV1InitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
