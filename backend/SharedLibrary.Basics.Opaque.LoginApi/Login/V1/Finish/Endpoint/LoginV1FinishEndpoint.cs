using SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Command;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Endpoint;

[Route("/v1/login/finish")]
[EndpointTag("Login")]
public sealed class LoginFinishV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
       app.MapPost<LoginV1FinishCommand, LoginV1FinishEndpointResponse>(this, LoginV1FinishHandlerAsync);
      //      .AllowAnonymous();
        return app;
    }

    private async Task<IResult> LoginV1FinishHandlerAsync(
        [FromBody] LoginV1FinishEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<LoginV1FinishEndpointRequest, LoginV1FinishCommand> requestMapper,
        [FromServices] IMapper<LoginV1FinishCommandResult, LoginV1FinishEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
