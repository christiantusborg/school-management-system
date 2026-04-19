using QuVian.CaseApi.CaseKeyPairs.V1.List.Command;

namespace QuVian.CaseApi.CaseKeyPairs.V1.List.Endpoint;

public class CaseKeyPairsV1ListEndpointResponse : HateoasBaseResponse
{
    public required List<CaseKeyPairItem> Items { get; init; }
    public required List<LabelOverrideItem> LabelOverrides { get; init; }
}
