namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Endpoint;

public class RegisterInitV1CreateEndpointResponse : HateoasBaseResponse
{
    public required Guid RegistrationId { get; init; } 
    public required byte[] EvaluatedElement  { get; init; }
}