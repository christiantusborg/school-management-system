namespace QuVian.CaseApi.CaseMembers.V1.GrantUser.Endpoint;

public class CaseMembersV1GrantUserEndpointRequest
{
    public required string TargetUserId { get; init; }
    public int Level { get; init; }

    /// <summary>Wrapped private keys for the target user, from Level through 6.</summary>
    public required List<WrappedLevelKeyDto> WrappedKeys { get; init; }
}

public class WrappedLevelKeyDto
{
    public int Level { get; init; }
    public required string KemCiphertext { get; init; }        // base64
    public required string EncryptedLevelPrivKey { get; init; } // base64
    public required string Nonce { get; init; }                 // base64
}
