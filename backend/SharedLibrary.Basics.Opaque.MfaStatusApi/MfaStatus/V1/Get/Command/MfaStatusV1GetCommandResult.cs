namespace SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Command;

public sealed class MfaStatusV1GetCommandResult : IMfaStatusV1GetCommandResultQueue
{
    public required MfaMethodInfoResult[] EnabledMethods { get; init; }
    public required Fido2CredentialInfoResult[] Fido2Credentials { get; init; }
}

public sealed class MfaMethodInfoResult
{
    public required string Method { get; init; }
}

public sealed class Fido2CredentialInfoResult
{
    public required Guid Fido2CredentialId { get; init; }
    public required string Label { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
}
