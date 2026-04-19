namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Command;

public sealed class EmailsV1CreateCommandResult : IEmailsV1CreateCommandResultQueue
{
    public required Guid UserContactEmailId { get; init; }
}
