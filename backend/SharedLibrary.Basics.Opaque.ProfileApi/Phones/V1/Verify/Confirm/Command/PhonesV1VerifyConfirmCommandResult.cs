namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Command;

public sealed class PhonesV1VerifyConfirmCommandResult : IPhonesV1VerifyConfirmCommandResultQueue
{
    public required Guid UserPhoneId { get; init; }
}
