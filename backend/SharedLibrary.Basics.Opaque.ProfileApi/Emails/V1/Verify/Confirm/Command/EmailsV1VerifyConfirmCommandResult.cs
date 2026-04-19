namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Command;

public sealed class EmailsV1VerifyConfirmCommandResult : IEmailsV1VerifyConfirmCommandResultQueue
{
    public required Guid UserContactEmailId { get; init; }
}
