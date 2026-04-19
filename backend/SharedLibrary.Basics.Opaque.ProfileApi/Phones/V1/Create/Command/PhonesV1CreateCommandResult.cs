namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Command;

public sealed class PhonesV1CreateCommandResult : IPhonesV1CreateCommandResultQueue
{
    public required Guid UserPhoneId { get; init; }
}
