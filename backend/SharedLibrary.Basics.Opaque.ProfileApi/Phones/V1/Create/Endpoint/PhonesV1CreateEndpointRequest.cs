namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Endpoint;

public class PhonesV1CreateEndpointRequest
{
    public required string Number { get; init; }
    public string? Label { get; init; }
}
