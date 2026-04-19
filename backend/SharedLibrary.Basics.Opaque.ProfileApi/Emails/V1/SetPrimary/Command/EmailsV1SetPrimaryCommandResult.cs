namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Command;

public sealed class EmailsV1SetPrimaryCommandResult : IEmailsV1SetPrimaryCommandResultQueue
{
    public required Guid UserContactEmailId { get; init; }
}
