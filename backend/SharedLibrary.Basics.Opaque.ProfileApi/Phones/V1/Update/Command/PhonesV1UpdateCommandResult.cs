namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Command;

public sealed class PhonesV1UpdateCommandResult : IPhonesV1UpdateCommandResultQueue
{
    public required Guid UserPhoneId { get; init; }
}
