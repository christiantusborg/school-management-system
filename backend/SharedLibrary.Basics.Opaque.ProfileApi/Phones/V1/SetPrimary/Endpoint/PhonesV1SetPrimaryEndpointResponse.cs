namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Endpoint;

public class PhonesV1SetPrimaryEndpointResponse : HateoasBaseResponse
{
    public required Guid UserPhoneId { get; init; }
}
