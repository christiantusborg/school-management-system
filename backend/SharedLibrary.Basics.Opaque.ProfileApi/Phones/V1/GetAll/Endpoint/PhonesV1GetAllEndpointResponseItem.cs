namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Endpoint;

public class PhonesV1GetAllEndpointResponseItem : HateoasBaseResponse
{
    public required Guid UserPhoneId { get; init; }
    public required string Number { get; init; }
    public string? Label { get; init; }
    public required bool IsPrimary { get; init; }
    public required bool IsVerified { get; init; }
}
