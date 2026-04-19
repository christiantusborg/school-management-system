namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Command;

public sealed class PhonesV1GetCommandResult : IPhonesV1GetCommandResultQueue
{
    public required Guid UserPhoneId { get; init; }
    public required string Number { get; init; }
    public string? Label { get; init; }
    public required bool IsPrimary { get; init; }
}
