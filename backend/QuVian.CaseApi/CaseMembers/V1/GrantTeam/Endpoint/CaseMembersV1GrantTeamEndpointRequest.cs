namespace QuVian.CaseApi.CaseMembers.V1.GrantTeam.Endpoint;

public class CaseMembersV1GrantTeamEndpointRequest
{
    public required Guid TeamId { get; init; }
    public int Level { get; init; }
    public required List<TeamWrappedLevelKeyDto> TeamWrappedKeys { get; init; }
}

public class TeamWrappedLevelKeyDto
{
    public int Level { get; init; }
    public required string EncryptedLevelPrivKey { get; init; }  // base64
    public required string Nonce { get; init; }                   // base64
}
