using QuVian.CaseApi.CaseMembers.V1.GrantTeam.Command;

namespace QuVian.CaseApi.CaseMembers.V1.GrantTeam.Endpoint;

[Route("/v1/cases/{caseId:guid}/members/teams")]
[EndpointTag("CaseMembers")]
public sealed class CaseMembersV1GrantTeamEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<CaseMembersV1GrantTeamCommand, CaseMembersV1GrantTeamEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId, [FromBody] CaseMembersV1GrantTeamEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CaseMembersV1GrantTeamCommandResult, CaseMembersV1GrantTeamEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CaseMembersV1GrantTeamCommand
        {
            CaseId          = caseId,
            TeamId          = request.TeamId,
            ActorUserId     = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            Level           = request.Level,
            TeamWrappedKeys = request.TeamWrappedKeys.Select(wk => new TeamWrappedLevelKey
            {
                Level                = wk.Level,
                EncryptedLevelPrivKey = Convert.FromBase64String(wk.EncryptedLevelPrivKey),
                Nonce                = Convert.FromBase64String(wk.Nonce)
            }).ToList()
        };
        var result = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return result.ToCreatedResult(responseMapper);
    }
}
