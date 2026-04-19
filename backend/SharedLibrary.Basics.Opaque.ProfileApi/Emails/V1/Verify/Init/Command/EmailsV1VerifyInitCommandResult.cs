namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Command;

public sealed class EmailsV1VerifyInitCommandResult : IEmailsV1VerifyInitCommandResultQueue
{
    public required Guid SessionId { get; init; }
}
