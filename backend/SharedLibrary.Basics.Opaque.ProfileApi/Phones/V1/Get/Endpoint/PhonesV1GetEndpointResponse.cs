namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Endpoint;

public class PhonesV1GetEndpointResponse : HateoasBaseResponse
{
    public required Guid UserPhoneId { get; init; }
    public required string Number { get; init; }
    public string? Label { get; init; }
    public required bool IsPrimary { get; init; }
}
