namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Endpoint;

public class RecoveryCodesV1GetStatusEndpointResponse : HateoasBaseResponse
{
    public required int RemainingCount { get; init; }
}
