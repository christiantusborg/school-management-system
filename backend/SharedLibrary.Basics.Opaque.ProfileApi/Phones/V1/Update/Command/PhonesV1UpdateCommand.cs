namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Command;

public sealed record PhonesV1UpdateCommand : IHandleableCommand<
    PhonesV1UpdateCommand,
    PhonesV1UpdateCommandValidator,
    PhonesV1UpdateCommandHandler,
    PhonesV1UpdateCommandResult>
{
    public required Guid UserPhoneId { get; init; }
    public required string UserId { get; init; }
    public required string Number { get; init; }
    public string? Label { get; init; }
}
