namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Command;

public sealed class EmailsV1UpdateCommandResult : IEmailsV1UpdateCommandResultQueue
{
    public required Guid UserContactEmailId { get; init; }
}
