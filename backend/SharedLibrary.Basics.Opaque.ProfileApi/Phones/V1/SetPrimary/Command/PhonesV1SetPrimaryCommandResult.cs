namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Command;

public sealed class PhonesV1SetPrimaryCommandResult : IPhonesV1SetPrimaryCommandResultQueue
{
    public required Guid UserPhoneId { get; init; }
}
