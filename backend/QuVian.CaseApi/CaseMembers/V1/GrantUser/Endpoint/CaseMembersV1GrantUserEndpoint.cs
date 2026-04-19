using QuVian.CaseApi.CaseMembers.V1.GrantUser.Command;

namespace QuVian.CaseApi.CaseMembers.V1.GrantUser.Endpoint;

[Route("/v1/cases/{caseId:guid}/members/users")]
[EndpointTag("CaseMembers")]
public sealed class CaseMembersV1GrantUserEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<CaseMembersV1GrantUserCommand, CaseMembersV1GrantUserEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId,
        [FromBody] CaseMembersV1GrantUserEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CaseMembersV1GrantUserCommandResult, CaseMembersV1GrantUserEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CaseMembersV1GrantUserCommand
        {
            CaseId       = caseId,
            TargetUserId = request.TargetUserId,
            ActorUserId  = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            Level        = request.Level,
            WrappedKeys  = request.WrappedKeys.Select(wk => new WrappedLevelKey
            {
                Level                = wk.Level,
                KemCiphertext        = Convert.FromBase64String(wk.KemCiphertext),
                EncryptedLevelPrivKey = Convert.FromBase64String(wk.EncryptedLevelPrivKey),
                Nonce                = Convert.FromBase64String(wk.Nonce)
            }).ToList()
        };
        var result = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return result.ToCreatedResult(responseMapper);
    }
}
