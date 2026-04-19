namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Command;

public sealed class PhonesV1VerifyInitCommandResult : IPhonesV1VerifyInitCommandResultQueue
{
    public required Guid SessionId { get; init; }
}
