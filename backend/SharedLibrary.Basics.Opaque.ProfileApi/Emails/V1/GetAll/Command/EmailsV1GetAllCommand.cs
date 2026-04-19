namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Command;

public sealed class EmailsV1GetAllCommand : IHandleableCommand<
    EmailsV1GetAllCommand,
    EmailsV1GetAllCommandValidator,
    EmailsV1GetAllCommandHandler,
    CommandSearchResult<EmailsV1GetAllCommandResultItem>>
{
    public required string UserId { get; init; }
}
