namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Command;

public sealed class EmailsV1DeleteCommandResult : IEmailsV1DeleteCommandResultQueue
{
    public required Guid UserContactEmailId { get; init; }
}
