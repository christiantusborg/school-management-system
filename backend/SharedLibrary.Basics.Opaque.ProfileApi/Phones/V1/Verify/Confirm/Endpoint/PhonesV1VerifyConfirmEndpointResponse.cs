namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Endpoint;

public class PhonesV1VerifyConfirmEndpointResponse : HateoasBaseResponse
{
    public required Guid UserPhoneId { get; init; }
}
