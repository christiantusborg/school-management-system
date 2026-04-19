using QuVian.CaseApi.CaseUserKeys.V1.GetMy.Command;

namespace QuVian.CaseApi.CaseUserKeys.V1.GetMy.Endpoint;

public class CaseUserKeysV1GetMyEndpointResponse : HateoasBaseResponse
{
    public required List<CaseUserKeyItem> Keys { get; init; }
}
