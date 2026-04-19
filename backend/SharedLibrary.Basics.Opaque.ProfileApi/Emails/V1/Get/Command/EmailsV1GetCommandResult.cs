namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get.Command;

public sealed class EmailsV1GetCommandResult : IEmailsV1GetCommandResultQueue
{
    public required Guid UserContactEmailId { get; init; }
    public required string Email { get; init; }
    public string? Label { get; init; }
    public required bool IsPrimary { get; init; }
}
