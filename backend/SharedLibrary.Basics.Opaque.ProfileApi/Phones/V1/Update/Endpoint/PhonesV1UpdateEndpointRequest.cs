namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Endpoint;

public class PhonesV1UpdateEndpointRequest
{
    public required string Number { get; init; }
    public string? Label { get; init; }
}
