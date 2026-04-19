namespace QuVian.CaseApi.CaseKeyPairs.V1.List.Command;

public sealed class CaseKeyPairsV1ListCommandResult : ICaseKeyPairsV1ListCommandResultQueue
{
    public required List<CaseKeyPairItem> Items { get; init; }
    public required List<LabelOverrideItem> LabelOverrides { get; init; }
}

public class CaseKeyPairItem
{
    public int Level { get; init; }
    public required string Name { get; init; }
    public required string PublicKey { get; init; }  // base64
}

public class LabelOverrideItem
{
    public int Level { get; init; }
    public required string Label { get; init; }
}
