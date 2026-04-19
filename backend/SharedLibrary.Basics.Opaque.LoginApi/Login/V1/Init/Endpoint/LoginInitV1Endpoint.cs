using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Endpoint;

[Route("/v1/login/init")]
[EndpointTag("Login")]
public sealed class LoginInitV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<LoginV1InitCommand, LoginV1InitEndpointResponse>(this, LoginV1InitHandlerAsync);
    //         .AllowAnonymous()
    //         .WithName(this.GetType().Name);
         return app;
    }

    private async Task<IResult> LoginV1InitHandlerAsync(
        [FromBody] LoginV1InitEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<LoginV1InitEndpointRequest, LoginV1InitCommand> requestMapper,
        [FromServices] IMapper<LoginV1InitCommandResult, LoginV1InitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
