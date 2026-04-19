namespace QuVian.CaseApi.CaseUserKeys.V1.GetMy.Command;

public sealed class CaseUserKeysV1GetMyCommandResult : ICaseUserKeysV1GetMyCommandResultQueue
{
    public required List<CaseUserKeyItem> Keys { get; init; }
}

public class CaseUserKeyItem
{
    public int Level { get; init; }
    public required string KemCiphertext { get; init; }       // base64
    public required string EncryptedLevelPrivKey { get; init; } // base64
    public required string Nonce { get; init; }                 // base64
}
