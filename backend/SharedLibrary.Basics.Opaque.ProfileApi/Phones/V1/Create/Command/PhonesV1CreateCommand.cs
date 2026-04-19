namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Command;

public sealed record PhonesV1CreateCommand : IHandleableCommand<
    PhonesV1CreateCommand,
    PhonesV1CreateCommandValidator,
    PhonesV1CreateCommandHandler,
    PhonesV1CreateCommandResult>
{
    public required string UserId { get; init; }
    public required string Number { get; init; }
    public string? Label { get; init; }
}
