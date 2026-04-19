namespace SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Endpoint;

public sealed class MfaStatusV1GetEndpointResponse : HateoasBaseResponse
{
    public required MfaStatusV1GetEndpointMethodInfo[] EnabledMethods { get; init; }
    public required MfaStatusV1GetEndpointFido2Info[] Fido2Credentials { get; init; }
}

public sealed class MfaStatusV1GetEndpointMethodInfo
{
    public required string Method { get; init; }
}

public sealed class MfaStatusV1GetEndpointFido2Info
{
    public required Guid Fido2CredentialId { get; init; }
    public required string Label { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
}
