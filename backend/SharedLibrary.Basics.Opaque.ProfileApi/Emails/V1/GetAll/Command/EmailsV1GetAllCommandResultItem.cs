using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Command;

public sealed class EmailsV1GetAllCommandResultItem : IEmailsV1GetAllCommandResultItemQueue
{
    public required Guid UserContactEmailId { get; init; }
    public required string Email { get; init; }
    public string? Label { get; init; }
    public required bool IsPrimary { get; init; }
    public required bool IsVerified { get; init; }
}
